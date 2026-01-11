namespace Bookify.Domain.Abstractions
{
    public abstract class Entity
    {
        protected Entity(Guid id)
        {
            Id = id;
        }
        public Guid Id { get; init; } // init means that when an entity is defined, the id is set for life
    }
}
