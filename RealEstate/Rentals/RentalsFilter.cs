using MongoDB.Driver;

namespace RealEstate.Rentals
{
	public class RentalsFilter
	{
		public decimal? PriceLimit { get; set; }
		public int? MinimumRooms { get; set; }

	    public FilterDefinition<Rental> ToFilterDefinition()
	    {
	        var filterDefinition = Builders<Rental>.Filter.Empty; // eq with .Find(new BsonDocument())  
	        //.Where(r => r.NumberOfRooms >= (filters.MinimumRooms ?? 0));
	        if (MinimumRooms.HasValue)
	        {
	            //var query = Query<Rental>.GTE(r => r.NumberOfRooms, filters.MinimumRooms);
	            //rentals = rentals.Where(r => query.Inject());
	            filterDefinition &= Builders<Rental>.Filter.Where(r => r.NumberOfRooms >= MinimumRooms.Value);
	            // the regular LINQ does not work with nullable! must specify explicily the .Value of a nullable!
	        }

	        if (PriceLimit.HasValue)
	        {
	            //var query = Query<Rental>.LTE(r => r.Price, filters.PriceLimit);
	            filterDefinition &= Builders<Rental>.Filter.Where(r => r.Price >= PriceLimit.Value);
	        }

	        return filterDefinition;
	    }
	}
}