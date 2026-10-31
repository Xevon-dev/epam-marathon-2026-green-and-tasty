using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Services.GuestSessions;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GroupReservations;

public class RemoveOwnerDishHandler(IGuestSessionService guestSessionService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var userId = UserContextHelper.GetAuthenticatedUserId(request);
        if (userId is null)
            return ResponseBuilder.CreateErrorResponse(401, "Unauthorized.", context.Logger);
        if (!request.TryGetPathValue("groupReservationId", out var groupReservationId) ||
            !request.TryGetPathValue("dishId", out var dishId))
            return ResponseBuilder.CreateErrorResponse(400, "Missing groupReservationId or dishId in path parameters.", context.Logger);

        try
        {
            await guestSessionService.RemoveOwnerDishAsync(userId, groupReservationId, dishId);
            return ResponseBuilder.CreateJsonResponse(200, new { message = "Dish removed from the owner's group order." });
        }
        catch (GroupReservationNotFoundException exception)
        {
            return ResponseBuilder.CreateErrorResponse(404, exception.Message, context.Logger);
        }
        catch (ActionForbiddenException exception)
        {
            return ResponseBuilder.CreateErrorResponse(403, exception.Message, context.Logger);
        }
        catch (ValidationException exception)
        {
            return ResponseBuilder.CreateErrorCodeResponse(exception);
        }
    }
}
