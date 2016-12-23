using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace Tests
{
    public class PocoTests
    {
        public PocoTests()
        {
            JsonWriterSettings.Defaults.Indent = true;
        }

        public class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }

            public List<string> Address = new List<string>();

            [BsonIgnoreIfNull]
            public Contact Contact = new Contact();

            [BsonIgnore]
            public string IgnoreMe { get; set; }

            [BsonElement("New")]
            public string Old { get; set; }

            [BsonElement]
            private string Encapsulated;

            [BsonRepresentation(BsonType.Double)]
            public decimal NetWorth { get; set; }
        }

        public class Contact
        {
            public string Email { get; set; }
            public string Phone { get; set; }
        }

        [Test]
        public void Automatic()
        {
            var person = new Person {Age = 54, Name = "bob"};
            person.Address.Add("101 some road");
            person.Address.Add("unit 504");

            person.Contact.Email = "email@email.com";
            person.Contact.Phone = "123-456-6789";

            Console.WriteLine(person.ToJson());
        }

        [Test]
        public void SerializationAttribute()
        {
            var person = new Person { Age = 54, Name = "bob" };
            person.Contact = null;
            person.NetWorth = 10.2M;
            Console.WriteLine(person.ToJson());
        }
    }
}
