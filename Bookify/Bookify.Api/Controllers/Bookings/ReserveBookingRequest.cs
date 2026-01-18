namespace Bookify.Api.Controllers.Bookings
{
    public sealed record ReserveBookingRequest(Guid UserId, Guid ApartmentId, DateOnly StartDate, DateOnly EndDate);
}
