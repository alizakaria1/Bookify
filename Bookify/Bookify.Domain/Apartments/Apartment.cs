using Bookify.Domain.Abstractions;
using Bookify.Domain.Shared;

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

        public Apartment()
        {
            
        }

        public Name Name { get; private set; } // the reason we are using private setters is because we don't want anyone to be able to change the vale of the property outside the class
        public Description Description { get; private set; }
        public Address Address { get; private set; } // this is a value object
        public Money Price { get; private set; }
        public Money CleaningFee { get; private set; }
        public DateTime? LastBookedOnUtc { get; internal set; } // we set it to internal to be able to set it's value in the Booking class
        public List<Amenity> Amenities { get; private set; } = new();
    }
}
