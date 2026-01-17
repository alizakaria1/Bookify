using Bookify.Application.Abstractions.Clock;
using Bookify.Application.Exceptions;
using Bookify.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Bookify.Infrastructure;

public sealed class ApplicationDbContext : DbContext, IUnitOfWork
{
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All
    };

    private readonly IDateTimeProvider _dateTimeProvider;

    public ApplicationDbContext(
        DbContextOptions options,
        IDateTimeProvider dateTimeProvider)
        : base(options)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly); // this will prevent us from adding any entity because it will take it automatically

        base.OnModelCreating(modelBuilder);
    }

    // here we are using EF core to publish our domain events
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            AddDomainEventsAsOutboxMessages();

            int result = await base.SaveChangesAsync(cancellationToken);

            // the reason we don't just publish the events after the saveChanges is because if the database operation succeeds,
            // there is a potential that the actions in the event fail. So we used the outbox pattern for a more robust way
            //await PublishDomainEventsAsync()

            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("Concurrency exception occurred.", ex);
        }
    }

    private void AddDomainEventsAsOutboxMessages()
    {
        var outboxMessages = ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                IReadOnlyList<IDomainEvent> domainEvents = entity.GetDomainEvents();

                entity.ClearDomainEvents(); // it's important to clear the domain events because when we publish domain events we don't know what would the event contain

                return domainEvents;
            });
            //.Select(domainEvent => new OutboxMessage(
            //    Guid.NewGuid(),
            //    _dateTimeProvider.UtcNow,
            //    domainEvent.GetType().Name,
            //    JsonConvert.SerializeObject(domainEvent, JsonSerializerSettings)))
            //.ToList();

        AddRange(outboxMessages);
    }

    //private async Task PublishDomainEventsAsync()
    //{
    //    var domainEvents = ChangeTracker.Entries<Entity>()
    //        .Select(entry => entry.Entity)
    //        .SelectMany(entity =>
    //        {
    //            var domainEvents = entity.GetDomainEvents();

    //            entity.ClearDomainEvents();

    //            return domainEvents;
    //        }).toList();

    //    foreach (var domainEvent in domainEvents)
    //    {
    //        await _publisher.Publish(domainEvent);
    //    }
    //}
}
