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
#pragma warning disable 618
            Database = client.GetServer().GetDatabase(Settings.Default.RealEstateDatabaseName);
#pragma warning restore 618

        }

        public MongoCollection<Rental> Rentals => Database.GetCollection<Rental>("rentals");
    }

    public class RealEstateContextNewApis
    {
        public readonly IMongoDatabase Database;

        public RealEstateContextNewApis()
        {
            var settings = MongoClientSettings.FromUrl(new MongoUrl(Settings.Default.RealEstateConnectionString));
            //settings.ClusterConfigurator = builder => builder.Subscribe<CommandStartedEvent>(started =>
            //{

            //});
            settings.ClusterConfigurator = builder => builder.Subscribe(new Log4NetMongoEvents());
            var client = new MongoClient(settings);
            Database = client.GetDatabase(Settings.Default.RealEstateDatabaseName);
        }

        public IMongoCollection<Rental> Rentals => Database.GetCollection<Rental>("rentals");
    }
}