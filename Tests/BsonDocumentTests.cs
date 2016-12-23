using System;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace Tests
{
    public class BsonDocumentTests
    {
        public BsonDocumentTests()
        {
            JsonWriterSettings.Defaults.Indent = true;
        }

        [Test]
        public void EmptyDocument()
        {
            var document = new BsonDocument();

            Console.WriteLine(document);
        }


        [Test]
        public void AddElements()
        {
            var document = new BsonDocument
            {
                {"age", new BsonInt32(54)},
                {"IsAlive", true}
            };
            document.Add("firstname", new BsonString("bob"));
            Console.WriteLine(document);
        }

        [Test]
        public void AddArrays()
        {
            var document = new BsonDocument();
            document.Add("address", new BsonArray(new[] {"101 Road", "Unit 504"}));

            Console.WriteLine(document);
        }

        [Test]
        public void EmbeddedDocument()
        {
            var document = new BsonDocument
            {
                {
                    "contact", new BsonDocument
                    {
                        {"Phone", "514564478"},
                        {"email", "whatever@email.com"}
                    }
                }
            };

            Console.WriteLine(document);
        }

        [Test]
        public void BsonValueConversions()
        {
            var document = new BsonDocument
            {
                {"age", 54}
            };

            Console.WriteLine(document["age"].ToDouble() + 10); // AsDouble throws invalidcast exception
            Console.WriteLine(document["age"].IsString);
            Console.WriteLine(document["age"].IsInt32);
        }

        [Test]
        public void ToBson()
        {
            var document = new BsonDocument
            {
                {"firstName", "bob"}
            };

            var bson = document.ToBson();

            Console.WriteLine(BitConverter.ToString(bson));

            var deserializedPerson = BsonSerializer.Deserialize<BsonDocument>(bson);
            Console.WriteLine(deserializedPerson);
        }
    }
}
