using Bookify.Domain.Apartments;
using Bookify.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookify.Infrastructure.Configurations;

internal sealed class ApartmentConfiguration : IEntityTypeConfiguration<Apartment>
{
    public void Configure(EntityTypeBuilder<Apartment> builder)
    {
        builder.ToTable("apartments");

        builder.HasKey(apartment => apartment.Id);

        // since the address is a value object, we use OwnsOne and what will happen now is that the properties in this Address
        // will be mapped to different columns in the parent entity in our case it is the apartment
        builder.OwnsOne(apartment => apartment.Address); 

        builder.Property(apartment => apartment.Name)
            .HasMaxLength(200)
            .HasConversion(name => name.Value, value => new Name(value));

        builder.Property(apartment => apartment.Description)
            .HasMaxLength(2000)
            .HasConversion(description => description.Value, value => new Description(value));

        builder.OwnsOne(apartment => apartment.Price, priceBuilder =>
        {
            priceBuilder.Property(money => money.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        builder.OwnsOne(apartment => apartment.CleaningFee, priceBuilder =>
        {
            priceBuilder.Property(money => money.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        // this is a shadow property on the apartment entity. and the IsRowVersion will tell EF code that this will be used to handle
        // concurrency to solve race condition issue.
        // we can follow the following two links which talk about concurrency
        // https://learn.microsoft.com/en-us/ef/core/saving/concurrency?tabs=data-annotations
        // https://www.npgsql.org/efcore/modeling/concurrency.html?tabs=data-annotations
        builder.Property<uint>("Version").IsRowVersion();
    }
}
