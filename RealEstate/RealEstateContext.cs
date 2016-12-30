using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using RealEstate.Properties;
using RealEstate.Rentals;

namespace RealEstate
{
    public class RealEstateContextNewApis
    {
        public readonly IMongoDatabase Database;

        public RealEstateContextNewApis()
        {
            var settings = MongoClientSettings.FromUrl(new MongoUrl(Settings.Default.LocalRealEstateConnectionString));  // RealEstateConnectionString
            //settings.ClusterConfigurator = builder => builder.Subscribe<CommandStartedEvent>(started =>
            //{

            //});
            settings.ClusterConfigurator = builder => builder.Subscribe(new Log4NetMongoEvents());
            Database = new MongoClient(settings).GetDatabase(Settings.Default.RealEstateDatabaseName);
            ImagesBucket = new GridFSBucket(Database, new GridFSBucketOptions());
        }

        public GridFSBucket ImagesBucket { get; set; }

        public IMongoCollection<Rental> Rentals => Database.GetCollection<Rental>("rentals");
    }
}