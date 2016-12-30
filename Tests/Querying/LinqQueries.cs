using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using RealEstate;
using Tests.Serialization;

namespace Tests.Querying
{
    using System;
    using System.Linq;
    using MongoDB.Driver.Builders;
    using MongoDB.Driver.Linq;
    using NUnit.Framework;
    //using RealEstate.App_Start;
    using RealEstate.Rentals;
    using Serialization;

    public class LinqQueries : DemoTests
    {
        [Test]
        public void Inject()
        {
            var query = Query<Rental>.LTE(r => r.Price, 500);

            var queryable = new RealEstateContextNewApis()
                .Rentals.AsQueryable()
                .Where(r => query.Inject());

            var translated = MongoQueryTranslator.Translate(queryable)
                as SelectQuery;
            Console.WriteLine(translated.BuildQuery());
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