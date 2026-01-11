using Bookify.Domain.Abstractions;

namespace Bookify.Domain.Apartments
{
    // making classes sealed if we don't want to inherit them in some cases can give some perforfance improvmenets
    // the notes folder contains some explanation on why we didn't just set strings and used value objects instead
    public sealed class Apartment : Entity
    {
        public Apartment(Guid id,
            Name name,
            Description description,
            Address address,
            Money price,
            Money cleaningFee,
            List<Amenity> amenities) : base(id)
        {
            Name = name;
            Description = description;
            Address = address;
            Price = price;
            CleaningFee = cleaningFee;
            Amenities = amenities;
        }

        public Name Name { get; set; }
        public Description Description { get; set; }
        public Address Address { get; set; } // this is a value object
        public Money Price { get; set; }
        public Money CleaningFee { get; set; }
        public DateTime? LastBookedOnUtc { get; set; }
        public List<Amenity> Amenities { get; set; } = new();
    }
}
