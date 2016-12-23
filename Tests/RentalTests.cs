using System;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using NUnit.Framework;
using RealEstate.Rentals;

namespace Tests
{
    [TestFixture]
    public class RentalTests : AssertionHelper
    {
        public RentalTests()
        {
            JsonWriterSettings.Defaults.Indent = true;
        }

        [Test]
        public void ToDocument_RentalWithPrice_PriceRepresentedAsDouble()
        {
            var rental = new Rental();
            rental.Price = 1;

            var document = rental.ToBsonDocument();

            Expect(document["Price"].BsonType, Is.EqualTo(BsonType.Double));
        }
        [Test]
        public void ToDocument_RentalWithId_IdRepresentedAsObjectId()
        {
            var rental = new Rental();
            rental.Id = ObjectId.GenerateNewId().ToString();

            var document = rental.ToBsonDocument();

            Expect(document["_id"].BsonType, Is.EqualTo(BsonType.ObjectId));
            Console.WriteLine(rental.ToJson());
        }
    }
}
