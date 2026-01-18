using Bookify.Domain.Abstractions;
using Bookify.Domain.Users.Events;

namespace Bookify.Domain.Users
{
    // the reason we set the constructor to private and create a factory method (create) is because we don't want to expost the
    // implementation outside the constructor, encapsulation, and to be able to introduce some side effects inside the factory method
    // that don't naturllay belong into a constructor
    public sealed class User : Entity
    {
        private User(Guid id, FirstName firstName, LastName lastName, Email email) : base(id)
        {
        }

        private User()
        {
            
        }

        public FirstName FirstName { get; private set; }
        public LastName LastName { get; private set; }
        public Email Email { get; private set; }

        public static User Create(FirstName firstName, LastName lastName, Email email)
        {
            var user = new User(Guid.NewGuid(), firstName, lastName, email);

            // the reason we did this is now when we want to persist a user in the database, an event will be published,
            // someone can subscribe to it and perform an operation async like sendind an email
            user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id));

            return user;
        }
    }
}
