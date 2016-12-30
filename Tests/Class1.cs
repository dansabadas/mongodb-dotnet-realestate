using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using RealEstate.Rentals;

namespace Tests
{
    public class RealEstateContext
    {
        public readonly MongoDatabase Database;

        public RealEstateContext()
        {
            var client = new MongoClient("");
#pragma warning disable 618
            Database = client.GetServer().GetDatabase("realestate");
#pragma warning restore 618

        }

        public MongoCollection<Rental> Rentals => Database.GetCollection<Rental>("rentals");
    }
}
