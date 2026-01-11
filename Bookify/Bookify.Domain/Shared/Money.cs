namespace Bookify.Domain.Shared
{
    public record Money(decimal Amount, Currency Currency)
    {
        //By using the syntax public static Money operator +, you are telling C# how to handle the plus sign when it is used between
        //two Money objects. Instead of the compiler saying "I don't know how to add two Money records together," it will now execute
        //the logic you wrote inside that method.
        //When you write code like var total = wallet + paycheck;, the compiler translates it behind the scenes to:
        //Money.operator +(wallet, paycheck).
        public static Money operator +(Money first, Money second)
        {
            if (first.Currency != second.Currency)
            {
                throw new InvalidOperationException("Currencies have to be equal");
            }

            return new Money(first.Amount + second.Amount, first.Currency);
        }

        public static Money Zero() => new(0, Currency.None);

        public static Money Zero(Currency currency) => new(0, currency);

        public bool IsZero() => this == Zero(Currency);

    }
}
