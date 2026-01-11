namespace Bookify.Domain.Apartments
{
    public record Currency
    {
        internal static readonly Currency None = new("");
        public static readonly Currency Usd = new("USD");
        public static readonly Currency Eur = new("EUR");

        private Currency(string code) => Code = code;

        public string Code { get; set; }

        public static Currency FromCode(string code)
        {
            return All.FirstOrDefault(x => x.Code == code) ?? throw new ApplicationException("The Currency Code is Invalid");
        }

        public static IReadOnlyCollection<Currency> All { get; set; } = new[]
        {
            None,
            Usd,
            Eur
        };
    }
}
