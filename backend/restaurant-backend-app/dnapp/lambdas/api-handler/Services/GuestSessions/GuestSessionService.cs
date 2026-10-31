using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Constants.Dishes;
using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Mappers;
using APIHandler.Models.GuestSessions;
using APIHandler.Models.GroupReservation;
using APIHandler.Models.Orders;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Reservations;
using APIHandler.Repositories.Tables;
using APIHandler.Services.GroupReservations;
using APIHandler.Services.Orders;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using APIHandler.Services.Notifications;

namespace APIHandler.Services.GuestSessions;

public class GuestSessionService(
    IGroupReservationRepository groupReservationRepository,
    IGuestSessionRepository guestSessionRepository,
    IDishRepository dishRepository,
    IReservationRepository reservationRepository,
    IUserRepository userRepository,
    ITableRepository tableRepository,
    IOrderService orderService,
    IGroupReservationService groupReservationService) : IGuestSessionService
{
    public async Task<bool> IsAuthorizedAsync(string guestSessionToken, string guestSessionId)
    {
        string hash = Convert.ToBase64String(SHA256.HashData(Convert.FromBase64String(guestSessionToken)));
        GuestSession? guestSession = await guestSessionRepository.GetByTokenHash(hash);
        if (guestSession is null)
            return false;

        return guestSession.Id == guestSessionId;
    }

    public async Task<CreatGuestSessionResponse> CreateGuestSessionAsync(CreateGuestSessionRequest request)
    {
        GroupReservation group = await GetGroupReservationAndValidateAsync(request.GroupReservationId);

        Task<Reservation?> reservationTask = reservationRepository.GetReservationByIdAsync(group.ReservationId);
        Task<int> countTask = guestSessionRepository.CountByGroupReservationId(group.GroupReservationId);

        Reservation reservation = await reservationTask ?? throw new GroupReservationExpiredException($"Associated reservation for group '{request.GroupReservationId}' not found.");

        if (reservation.Status != ReservationStatus.Reserved)
        {
            throw new GuestSessionExpiredException($"This pre-order session has expired or is no longer active. Current reservation status: '{reservation.Status}'.");
        }

        int joinedGuestsCount = await countTask;

        if (joinedGuestsCount + 1 >= reservation.GuestCount)
            throw new GuestLimitExceededException($"Guest limit exceeded. Maximum capacity is {reservation.GuestCount}, but {joinedGuestsCount} guests have already joined.");

        byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);

        var session = new GuestSession()
        {
            Id = Guid.NewGuid().ToString(),
            GuestName = request.GuestName,
            GroupReservationId = group.GroupReservationId,
            SessionTokenHash = Convert.ToBase64String(SHA256.HashData(tokenBytes)),
            Dishes = [],
        };

        await guestSessionRepository.SaveAsync(session);

        return new CreatGuestSessionResponse(session.Id, Convert.ToBase64String(tokenBytes));
    }

    public async Task<GuestMenuResponse> GetGuestMenuAsync(string guestSessionId)
    {
        var session = await guestSessionRepository.GetByGuestSessionId(guestSessionId)
            ?? throw new ValidationException($"Guest session '{guestSessionId}' not found.");

        await ValidateGroupReservationId(session.GroupReservationId);

        string locationId = await GetLocationIdForGroupAsync(session.GroupReservationId);
        var dishes = await dishRepository.GetDishesByLocationIdAsync(locationId);

        var dishesMapped = dishes
            .GroupBy(d => d.DishType)
            .ToDictionary(k => k.Key, GuestMenuDishMapper.Map);

        return new GuestMenuResponse(dishesMapped);
    }

    public async Task AddDishAsync(string guestSessionId, string dishId, int quantity)
    {
        if (quantity <= 0)
            throw new ValidationException($"Quantity ({quantity}) must be greater than zero.");

        GuestSession session = await GetGuestSessionAsync(guestSessionId);

        string locationId = await GetLocationIdForGroupAsync(session.GroupReservationId);
        var locationDishes = await dishRepository.GetDishesByLocationIdAsync(locationId);

        var dish = locationDishes.FirstOrDefault(d => d.Id == dishId)
            ?? throw new ValidationException($"Dish '{dishId}' does not exist or is not available at this location.");

        if (dish.State != DishState.Available)
            throw new DishIsNotAvailableException($"Dish '{dishId}' is currently unavailable. State: '{dish.State}'.");

        session.Dishes ??= [];

        var existingDish = session.Dishes.FirstOrDefault(d => d.DishId == dishId);

        if (existingDish != null)
        {
            existingDish.Quantity += quantity;
        }
        else
        {
            session.Dishes.Add(new OrderItem
            {
                DishId = dishId,
                Quantity = quantity,
                State = OrderItemState.Pending
            });
        }

        await guestSessionRepository.SaveAsync(session);
    }

    public async Task RemoveDishAsync(string guestSessionId, string dishId, int quantity)
    {
        if (quantity <= 0)
            throw new ValidationException("Quantity must be greater than zero.");

        GuestSession session = await GetGuestSessionAsync(guestSessionId);

        if (session.Dishes == null || session.Dishes.Count == 0)
            throw new DishNotFoundException($"Dish {dishId} not found in guest session.");

        var existingDish = session.Dishes.FirstOrDefault(d => d.DishId == dishId)
            ?? throw new DishNotFoundException($"Dish {dishId} not found in guest session.");

        existingDish.Quantity -= quantity;

        if (existingDish.Quantity <= 0)
        {
            session.Dishes.Remove(existingDish);
        }

        await guestSessionRepository.SaveAsync(session);
    }

    public async Task<GroupReservationStatusResponse> GetGroupReservationStatusAsync(string groupReservationId)
    {
        var group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId)
            ?? throw new GroupReservationNotFoundException();

        var reservation = await reservationRepository.GetReservationByIdAsync(group.ReservationId)
            ?? throw new InvalidOperationException("Associated reservation not found.");

        int currentParticipants = await GetCurrentParticipantCountAsync(groupReservationId);

        return new GroupReservationStatusResponse
        {
            CurrentParticipants = currentParticipants,
            MaxCapacity = reservation.GuestCount,
            Status = group.Status
        };
    }

    
    public async Task<GetAllSessionsResponse> DeleteGuestSessionAsync(string userId, string groupReservationId, string guestSessionId)
    {
        await ValidateOpenReservationOwnerAsync(userId, groupReservationId);

        var session = await guestSessionRepository.GetByGuestSessionId(guestSessionId)
            ?? throw new GuestSessionNotFoundException($"Guest session with id {guestSessionId} not found.");

        if (session.GroupReservationId != groupReservationId)
        {
            throw new ValidationException("Guest session does not belong to this group reservation.");
        }

        if (session.IsOwnerSession)
        {
            throw new ValidationException("The reservation owner's session cannot be removed from the group.");
        }

        await guestSessionRepository.DeleteAsync(session);

        var sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(groupReservationId);

        return await BuildSessionSnapshotAsync(sessions);
    }

    public async Task<GetAllSessionsResponse> GetSessionsForOwnerAsync(string userId, string groupReservationId)
    {
        await ValidateReservationOwnerAsync(groupReservationId, userId);
        var sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(groupReservationId);
        return await BuildSessionSnapshotAsync(sessions);
    }

    public async Task<OwnerSessionResponse> GetOwnerSessionIdAsync(string userId, string reservationId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ActionForbiddenException("Only the reservation owner can access the group order.");

        var reservation = await reservationRepository.GetReservationByIdAsync(reservationId)
            ?? throw new ReservationNotFoundException($"Reservation {reservationId} was not found.");

        if (!string.Equals(reservation.CustomerId, userId, StringComparison.Ordinal))
            throw new ActionForbiddenException("Only the reservation owner can access the group order.");

        var group = await groupReservationRepository.GetByReservationIdAsync(reservationId)
            ?? throw new GroupReservationNotFoundException();

        if (group.Status == GroupReservationStatus.Closed ||
            (group.Status != GroupReservationStatus.Submitted && reservation.Status != ReservationStatus.Reserved))
            throw new GroupReservationExpiredException("The group pre-order is no longer open.");

        var sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(group.GroupReservationId);
        var ownerSession = sessions.FirstOrDefault(session =>
            session.IsOwnerSession && string.Equals(session.OwnerUserId, userId, StringComparison.Ordinal));

        if (ownerSession is null)
        {
            var deterministicOwnerSession = await guestSessionRepository.GetByGuestSessionId($"owner-{group.GroupReservationId}");
            if (deterministicOwnerSession?.IsOwnerSession == true &&
                string.Equals(deterministicOwnerSession.OwnerUserId, userId, StringComparison.Ordinal))
            {
                ownerSession = deterministicOwnerSession;
            }
        }

        if (ownerSession is null && group.Status == GroupReservationStatus.Submitted)
            throw new GroupReservationSubmittedException();

        if (ownerSession is null)
        {
            ownerSession = await CreateGroupOwnerSessionAsync(userId, group.GroupReservationId);
        }

        return new OwnerSessionResponse(ownerSession.Id, group.GroupReservationId, group.Status.ToString());
    }

    public async Task<GuestMenuResponse> GetOwnerMenuAsync(string userId, string groupReservationId)
    {
        await ValidateOpenReservationOwnerAsync(userId, groupReservationId);
        var locationId = await GetLocationIdForGroupAsync(groupReservationId);
        var dishes = await dishRepository.GetDishesByLocationIdAsync(locationId);
        var dishesByCategory = dishes
            .GroupBy(dish => dish.DishType)
            .ToDictionary(group => group.Key, GuestMenuDishMapper.Map);
        return new GuestMenuResponse(dishesByCategory);
    }

    public async Task AddOwnerDishAsync(string userId, string groupReservationId, string dishId, int quantity)
    {
        await ValidateOpenReservationOwnerAsync(userId, groupReservationId);
        if (string.IsNullOrWhiteSpace(dishId) || quantity <= 0)
            throw new ValidationException("A valid dish id and a quantity greater than zero are required.");

        var dish = await dishRepository.GetDishByIdAsync(dishId)
            ?? throw new DishNotFoundException($"Dish with id '{dishId}' was not found.");
        if (dish.State != DishState.Available)
            throw new DishIsNotAvailableException($"The dish '{dish.Name}' ({dish.Id}) is not available.");

        var session = await GetOrCreateOwnerSessionAsync(userId, groupReservationId);
        var existingItem = session.Dishes.FirstOrDefault(item => item.DishId == dishId);
        if (existingItem is null)
        {
            session.Dishes.Add(new OrderItem { DishId = dishId, Quantity = quantity });
        }
        else
        {
            existingItem.Quantity += quantity;
        }

        session.IsSubmitted = false;
        await guestSessionRepository.SaveAsync(session);
    }

    public async Task RemoveOwnerDishAsync(string userId, string groupReservationId, string dishId)
    {
        await ValidateOpenReservationOwnerAsync(userId, groupReservationId);
        if (string.IsNullOrWhiteSpace(dishId))
            throw new ValidationException("Dish id is required.");

        var session = await GetOrCreateOwnerSessionAsync(userId, groupReservationId);
        session.Dishes.RemoveAll(item => item.DishId == dishId);
        session.IsSubmitted = false;
        await guestSessionRepository.SaveAsync(session);
    }

    public async Task SubmitOwnerSessionAsync(string userId, string groupReservationId)
    {
        await ValidateOpenReservationOwnerAsync(userId, groupReservationId);
        var session = await GetOrCreateOwnerSessionAsync(userId, groupReservationId);
        if (session.Dishes.Count == 0)
            throw new CartIsEmptyException();

        await ValidateDishesAsync(session.Dishes);
        session.IsSubmitted = true;
        await guestSessionRepository.SaveAsync(session);
    }

    public async Task<IReadOnlyList<OrderItem>> SubmitGroupPreOrderAsync(
        string userId,
        string groupReservationId,
        string reservationId,
        ILambdaContext context)
    {
        await ValidateReservationOwnerAsync(groupReservationId, userId);

        var group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId)
            ?? throw new GroupReservationNotFoundException();
        if (!string.Equals(group.ReservationId, reservationId, StringComparison.Ordinal))
            throw new ValidationException("The reservation does not belong to this group order.");
        if (group.Status != GroupReservationStatus.Open)
            throw new GroupReservationSubmittedException();

        _ = await reservationRepository.GetReservationByIdAsync(reservationId)
            ?? throw new ReservationNotFoundException($"Reservation {reservationId} was not found.");
        var order = await orderService.GetOrderByReservationIdAsync(reservationId);
        if (order is null)
            throw new ValidationException("Pre-order for the reservation was not found.");

        var sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(groupReservationId);
        var submittedSessions = sessions.Where(session => session.IsSubmitted).ToArray();
        if (submittedSessions.Length == 0)
            throw new ValidationException("No participant has submitted their pre-order.");

        var combinedItems = submittedSessions
            .SelectMany(session => session.Dishes)
            .GroupBy(item => item.DishId)
            .Select(grouped => new OrderItem
            {
                DishId = grouped.Key,
                Quantity = grouped.Sum(item => item.Quantity),
            })
            .ToList();

        if (combinedItems.Count == 0)
            throw new ValidationException("Submitted participant carts do not contain any dishes.");

        await orderService.EditPreOrderAsync(userId, reservationId, combinedItems, context);
        await orderService.SubmitPreOrderAsync(userId, reservationId, context);
        await groupReservationService.UpdateGroupReservationStatusAsync(groupReservationId, GroupReservationStatus.Submitted);

        return combinedItems;
    }

    private async Task<GetAllSessionsResponse> BuildSessionSnapshotAsync(IReadOnlyCollection<GuestSession> sessions)
    {

        var dishIds = sessions
            .SelectMany(s => s.Dishes)
            .Select(s => s.DishId)
            .Distinct()
            .ToArray();

        var allDishes = await dishRepository.GetDishesByIdsAsync(dishIds);
        var dishesById = allDishes.ToDictionary(d => d.Id);

        var participants = new List<Participant>();

        foreach (var guestSession in sessions)
        {
            var items = guestSession.Dishes
                .Where(dish => dishesById.ContainsKey(dish.DishId))
                .Select(dish => new OrderItemResponse
                {
                    Dish = DishMapper.ToDishResponse(dishesById[dish.DishId]),
                    Quantity = dish.Quantity
                }).ToArray();

            decimal total = items.Sum(i => i.Quantity * i.Dish.Price);
            var participantType = guestSession.IsOwnerSession ? ParticipantType.Owner : ParticipantType.Guest;
            var participantName = guestSession.IsOwnerSession ? "You (owner)" : guestSession.GuestName;
            var participant = new Participant(guestSession.Id, participantType, participantName, items, total, guestSession.IsSubmitted);
            participants.Add(participant);
        }

        return new GetAllSessionsResponse(participants, participants.Sum(p => p.Total));
    }
    public async Task SubmitCartAsync(string guestSessionId)
    {
        GuestSession guestSession = await GetGuestSessionAsync(guestSessionId);

        if (!guestSession.Dishes.Any())
            throw new CartIsEmptyException();

        await ValidateDishesAsync(guestSession.Dishes);

        guestSession.IsSubmitted = true;
        await guestSessionRepository.SaveAsync(guestSession);
    }

    private async Task<GroupReservation> GetGroupReservationAndValidateAsync(string groupReservationId)
    {
        GroupReservation? group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId);
        ValidateGroupReservation(group);
        return group;
    }

    private async Task<int> GetCurrentParticipantCountAsync(string groupReservationId)
    {
        var sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(groupReservationId);

        // Owner is one reservation seat but also has a record in the sessions table.
        // Count guest sessions only, then add the owner once.
        return (sessions?.Count(session => !session.IsOwnerSession) ?? 0) + 1;
    }

    private async Task ValidateGroupReservationId(string groupReservationId) => await GetGroupReservationAndValidateAsync(groupReservationId);

    private static void ValidateGroupReservation([NotNull] GroupReservation? group)
    {
        if (group is null)
            throw new GroupReservationNotFoundException();

        if (group.Status == GroupReservationStatus.Closed)
            throw new GroupReservationExpiredException();

        if (group.Status == GroupReservationStatus.Submitted)
            throw new GroupReservationSubmittedException();
    }

    private async Task<GuestSession> GetGuestSessionAsync(string sessionId)
    {
        var session = await guestSessionRepository.GetByGuestSessionId(sessionId)
            ?? throw new GuestSessionNotFoundException($"Guest session {sessionId} not found.");

        if (session.IsSubmitted)
            throw new GuestSessionSubmittedException();

        await ValidateGroupReservationId(session.GroupReservationId);

        return session;
    }

    private async Task ValidateDishesAsync(IEnumerable<OrderItem> orders)
    {
        IEnumerable<string> dishIds = orders.Select(o => o.DishId).ToArray();
        IEnumerable<Dish> dishes = await dishRepository.GetDishesByIdsAsync(dishIds);

        if (dishes.FirstOrDefault(d => d.State != DishState.Available) is Dish dish)
            throw new DishIsNotAvailableException($"The dish '{dish.Name}' ({dish.Id}) is not available.");
    }

    private async Task<string> GetLocationIdForGroupAsync(string groupReservationId)
    {
        var group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId)
            ?? throw new GroupReservationNotFoundException();

        var reservation = await reservationRepository.GetReservationByIdAsync(group.ReservationId)
            ?? throw new ReservationNotFoundException("Parent reservation not found.");

        var table = await tableRepository.GetTableAsync(reservation.TableId)
            ?? throw new ValidationException("Associated table not found.");

        return table.LocationId;
    }

    
    private async Task ValidateReservationOwnerAsync(string groupReservationId, string userId)
    {
        var group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId)
            ?? throw new GroupReservationNotFoundException();

        var reservation = await reservationRepository.GetReservationByIdAsync(group.ReservationId)
            ?? throw new ReservationNotFoundException("Parent reservation not found.");

        if (reservation.CustomerId != userId)
        {
            throw new ActionForbiddenException("Only the reservation owner can perform this action.");
        }
    }

    private async Task<GuestSession> CreateGroupOwnerSessionAsync(string userId, string groupReservationId)
    {
        var ownerSessionId = $"owner-{groupReservationId}";
        var existingSession = await guestSessionRepository.GetByGuestSessionId(ownerSessionId);
        if (existingSession is not null)
        {
            if (!existingSession.IsOwnerSession || !string.Equals(existingSession.OwnerUserId, userId, StringComparison.Ordinal))
                throw new ActionForbiddenException("The owner session does not belong to the authenticated user.");
            return existingSession;
        }

        var session = new GuestSession
        {
            Id = ownerSessionId,
            GroupReservationId = groupReservationId,
            GuestName = "Owner",
            Dishes = [],
            IsOwnerSession = true,
            OwnerUserId = userId,
        };

        await guestSessionRepository.SaveAsync(session);
        return session;
    }

    private async Task<GuestSession> GetOrCreateOwnerSessionAsync(string userId, string groupReservationId)
    {
        var sessionId = $"owner-{groupReservationId}";
        var session = await guestSessionRepository.GetByGuestSessionId(sessionId);
        if (session is not null)
        {
            if (!session.IsOwnerSession || !string.Equals(session.OwnerUserId, userId, StringComparison.Ordinal))
                throw new ActionForbiddenException("The owner session does not belong to the authenticated user.");
            return session;
        }

        return await CreateGroupOwnerSessionAsync(userId, groupReservationId);
    }

    private async Task ValidateOpenReservationOwnerAsync(string userId, string groupReservationId)
    {
        await ValidateReservationOwnerAsync(groupReservationId, userId);
        var group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId)
            ?? throw new GroupReservationNotFoundException();
        if (group.Status != GroupReservationStatus.Open)
            throw new GroupReservationSubmittedException();

        var reservation = await reservationRepository.GetReservationByIdAsync(group.ReservationId)
            ?? throw new ReservationNotFoundException("Associated reservation was not found.");
        if (reservation.Status != ReservationStatus.Reserved)
            throw new GroupReservationExpiredException("The reservation is no longer active.");
    }

    public async Task<GetGuestSessionResponse> GetGuestSessionInfoAsync(string guestSessionId)
    {
        var session = await guestSessionRepository.GetByGuestSessionId(guestSessionId)
            ?? throw new GuestSessionNotFoundException("Guest session was not found.");

        var dishIds = session.Dishes
            .Select(s => s.DishId)
            .Distinct()
            .ToArray();

        var allDishes = await dishRepository.GetDishesByIdsAsync(dishIds);
        var dishesById = allDishes.ToDictionary(d => d.Id);

        var items = session.Dishes.Select(d => new OrderItemResponse
        {
            Dish = DishMapper.ToDishResponse(dishesById[d.DishId]),
            Quantity = d.Quantity
        }).ToArray();

        decimal total = items.Sum(i => i.Quantity * i.Dish.Price);

        string name = session.GuestName;
        if (session.IsOwnerSession)
        {
            var groupReservation = await groupReservationRepository.GetGroupReservationByIdAsync(session.GroupReservationId);
            var reservation = await reservationRepository.GetReservationByIdAsync(groupReservation!.ReservationId);
            var user = await userRepository.GetUserByIdAsync(reservation!.CustomerId!);
            name = user!.Attributes.First(a => a.Name == "given_name").Value;
        }

        return new GetGuestSessionResponse(name, items, total, session.IsSubmitted);
    }

}
