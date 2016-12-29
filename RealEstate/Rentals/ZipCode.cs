using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.Rentals
{
    [BsonIgnoreExtraElements]
    public class ZipCode
    {
        [BsonId]
        public string Id { get; set; }

        [BsonElement("city")]
        public string City { get; set; }

        [BsonElement("state")]
        public string State { get; set; }
    }
}