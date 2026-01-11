using MediatR;

namespace Bookify.Domain.Abstractions
{
    // this interface will include all the domain events in the system.
    // A domain event is something of significance that has occured in the domain and you want to notify other components about it
    // we will implement this using MediatR package
    public interface IDomainEvent : INotification
    {
    }
}
