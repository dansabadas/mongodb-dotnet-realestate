using MongoDB.Driver;
using RealEstate.Properties;
using RealEstate.Rentals;

namespace RealEstate
{
    public class RealEstateContext
    {
        public readonly MongoDatabase Database;

        public RealEstateContext()
        {
            var client = new MongoClient(Settings.Default.LocalRealEstateConnectionString);
            Database = client.GetServer().GetDatabase(Settings.Default.RealEstateDatabaseName);

        }

        public MongoCollection<Rental> Rentals => Database.GetCollection<Rental>("rentals");
    }

    public class RealEstateContextNewApis
    {
        public readonly IMongoDatabase Database;

        public RealEstateContextNewApis()
        {
            var client = new MongoClient(Settings.Default.LocalRealEstateConnectionString);
            Database = client.GetDatabase(Settings.Default.RealEstateDatabaseName);
        }

        public IMongoCollection<Rental> Rentals => Database.GetCollection<Rental>("rentals");
    }
}