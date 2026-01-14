using Bookify.Application.Abstractions.Clock;
using Bookify.Application.Abstractions.Messaging;
using Bookify.Domain.Abstractions;
using Bookify.Domain.Apartments;
using Bookify.Domain.Bookings;
using Bookify.Domain.Users;
using System.Data;

namespace Bookify.Application.Bookings.ReserveBooking
{
    // here we are saying that the ReserveBookingCommandHandler handles the ReserveBookingCommand and returns a Guid which is the Guid of the new booking
    internal sealed class ReserveBookingCommandHandler : ICommandHandler<ReserveBookingCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IApartmentRepository _apartmentRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PricingService _pricingService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ReserveBookingCommandHandler(
            IUserRepository userRepository,
            IApartmentRepository apartmentRepository,
            IBookingRepository bookingRepository,
            IUnitOfWork unitOfWork,
            PricingService pricingService,
            IDateTimeProvider dateTimeProvider)
        {
            _userRepository = userRepository;
            _apartmentRepository = apartmentRepository;
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _pricingService = pricingService;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<Guid>> Handle(ReserveBookingCommand request, CancellationToken cancellationToken)
        {
            User? user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            if (user is null)
            {
                return Result.Failure<Guid>(UserErrors.NotFound);
            }

            Apartment? apartment = await _apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

            if (apartment is null)
            {
                return Result.Failure<Guid>(ApartmentErrors.NotFound);
            }

            var duration = DateRange.Create(request.StartDate, request.EndDate);

            // below if we think about it is an issue related to concurrency and race conditions. So if two transactions are happening
            // and both pass this check because there is no booking for that apartment, then both will try to save the booking in the
            // database.
            // there are two ways to solve this, 
            // 1) pessimisting locking : creating a transaction with more constrictive isolation level
            // 2) optimistic locking : having a concurrency token present on the entities
            // 2 is used here because it is more performant and doesn't require locking certain rows in the database for extended periods
            if (await _bookingRepository.IsOverlappingAsync(apartment, duration, cancellationToken))
            {
                return Result.Failure<Guid>(BookingErrors.Overlap);
            }

            //try
            //{
                var booking = Booking.Reserve(
                    apartment,
                    user.Id,
                    duration,
                    _dateTimeProvider.UtcNow,
                    _pricingService);

                _bookingRepository.Add(booking);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return booking.Id;
            //}
            //catch (ConcurrencyException)
            //{
            //    return Result.Failure<Guid>(BookingErrors.Overlap);
            //}
        }
    }
}
