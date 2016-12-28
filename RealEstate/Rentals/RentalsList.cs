namespace RealEstate.Rentals
{
	using System.Collections.Generic;

	public class RentalsList
	{
		public IEnumerable<RentalViewModel> Rentals { get; set; }
		public RentalsFilter Filters { get; set; }
	}

    public class RentalViewModel
    {
        public string Id { get; set; }

        public string Description { get; set; }

        public int NumberOfRooms { get; set; }

        public List<string> Address { get; set; }

        public decimal Price { get; set; }

    }
}