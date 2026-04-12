using Bookify.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bookify.Domain.UnitTests.Infrastructure
{
    public abstract class BaseTest
    {
        public static T AssertDomainEventWasPublished<T>(Entity entity)
            where T : IDomainEvent
        {
            T? domainEvent = entity.GetDomainEvents().OfType<T>().SingleOrDefault();

            if (domainEvent == null)
            {
                throw new Exception($"{typeof(T).Name} was not published");
            }

            return domainEvent;
        }
    }
}
