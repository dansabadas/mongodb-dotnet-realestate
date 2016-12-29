using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
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

        public IEnumerable<BsonDocument> Run(MongoCollection<Rental> rentals)
        {
            var priceRange = new BsonDocument(
                "$subtract",
                new BsonArray
                {
                    "$Price",
                    new BsonDocument("$mod", new BsonArray{"$Price", 500})
                });

            var grouping = new BsonDocument(
                "$group",
                new BsonDocument
                {
                    {"_id", priceRange},
                    {"count", new BsonDocument("$sum", 1)}
                });

            var sort = new BsonDocument(
                "$sort",
                new BsonDocument("_id", 1)
                );

            var args = new AggregateArgs
            {
                Pipeline = new[] { grouping, sort }
            };

            return rentals.Aggregate(args);
        }
    }
}