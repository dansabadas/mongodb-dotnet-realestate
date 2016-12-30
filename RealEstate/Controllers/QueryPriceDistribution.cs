using System.Collections;
using System.Linq;
using MongoDB.Driver;

namespace RealEstate.Rentals
{
    public class QueryPriceDistribution
    {
        public IEnumerable RunAggregationFluent(IMongoCollection<Rental> rentals)
        {
            var distributions = rentals.Aggregate()
                .Project(r => new {r.Price, PriceRange = (double)r.Price - (double)r.Price % 500 })
                .Group(r => r.PriceRange, g => new { GroupPriceRange = g.Key, Count = g.Count() })
                .SortBy(p => p.GroupPriceRange)
                .ToList();

            return distributions;
        }

        public IEnumerable RunLinq(IMongoCollection<Rental> rentals)
        {
            var distributions = rentals.AsQueryable()
                .Select(r => new { r.Price, PriceRange = (double)r.Price - (double)r.Price % 500 })
                .GroupBy(r => r.PriceRange)
                .Select(g => new { GroupPriceRange = g.Key, Count = g.Count() })
                .OrderBy(p => p.GroupPriceRange)
                .ToList();

            return distributions;
        }
    }
}