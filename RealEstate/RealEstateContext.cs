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
            var server = client.GetServer();
            Database = server.GetDatabase(Settings.Default.RealEstateDatabaseName);
        }

        public MongoCollection<Rental> Rentals => Database.GetCollection<Rental>("rentals");
    }
}