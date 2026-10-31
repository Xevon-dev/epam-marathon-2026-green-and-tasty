using APIHandler.Models.GuestSessions;
using APIHandler.Models.GroupReservation;
using APIHandler.Entities;
using Amazon.Lambda.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Services.GuestSessions;

public interface IGuestSessionService
{
    Task<CreatGuestSessionResponse> CreateGuestSessionAsync(CreateGuestSessionRequest request);
    Task AddDishAsync(string guestSessionId, string dishId, int quantity);
    Task RemoveDishAsync(string guestSessionId, string dishId, int quantity);
    Task<GroupReservationStatusResponse> GetGroupReservationStatusAsync(string groupReservationId);
    Task<GetAllSessionsResponse> GetSessionsForOwnerAsync(string userId, string groupReservationId);
    Task<OwnerSessionResponse> GetOwnerSessionIdAsync(string userId, string reservationId);
    Task<GuestMenuResponse> GetOwnerMenuAsync(string userId, string groupReservationId);
    Task AddOwnerDishAsync(string userId, string groupReservationId, string dishId, int quantity);
    Task RemoveOwnerDishAsync(string userId, string groupReservationId, string dishId);
    Task SubmitOwnerSessionAsync(string userId, string groupReservationId);
    Task<IReadOnlyList<OrderItem>> SubmitGroupPreOrderAsync(string userId, string groupReservationId, string reservationId, ILambdaContext context);
    Task<bool> IsAuthorizedAsync(string guestSessionToken, string guestSessionId);
    Task SubmitCartAsync(string guestSessionId);
    Task<GuestMenuResponse> GetGuestMenuAsync(string guestSessionId);
    Task<GetAllSessionsResponse> DeleteGuestSessionAsync(string userId, string groupReservationId, string guestSessionId);
    Task<GetGuestSessionResponse> GetGuestSessionInfoAsync(string guestSessionId);
}
