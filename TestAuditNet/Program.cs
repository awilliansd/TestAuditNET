using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using Audit.Core;
using Audit.MongoDB.Providers;
using Audit.SqlServer;
using Audit.SqlServer.Providers;
using Dapper;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace TestAuditNet
{
    class Program
    {
        static void Main(string[] args)
        {
            var mongoDataProvider = new MongoDataProvider()
            {
                ConnectionString = "mongodb://localhost:27017",
                Database = "Audit",
                Collection = "Event"
            };

            var sqlDataProvider = new SqlDataProvider()
            {
                ConnectionString = "data source=localhost;initial catalog=Auditoria;integrated security=true;",
                Schema = "dbo",
                TableName = "Event",
                IdColumnName = "EventId",
                JsonColumnName = "JsonData",
                LastUpdatedDateColumnName = "LastUpdatedDate",
                CustomColumns = new List<CustomColumn>()
                {
                    new CustomColumn("EventType", ev => ev.EventType),
                    new CustomColumn("User", ev => ev.Environment.UserName)
                }
            };

            Configuration.DataProvider = sqlDataProvider;

            Console.WriteLine("Start");

            var order = new Order
            {
                Status = EnumStatus.Start
            };

            //Console.WriteLine(DateTime.Now.ToString());

            var i = 0;
            //while (i < 10000)
            //{
            //    using (var audit = AuditScope.Create("Order:Update", () => order, new { Perfil = "Willian", Configuration = "192.168.1.1" }))
            //    {
            //        audit.Event.StartDate = DateTime.Now;
            //        order.Status = EnumStatus.Finish;

            //        audit.Event.CustomFields["ReferenceId"] = 111;
            //        audit.Event.StartDate = DateTime.Now;

            //        audit.Comment("Salvar a ordem1");
            //        audit.Event.EndDate = DateTime.Now;
            //    }
            //    i++;
            //}

            var startTime = DateTime.Now;
            using (var conexao = new SqlConnection("data source=localhost;initial catalog=Auditoria;integrated security=true;"))
            {
                var test = conexao.Query<dynamic>("SELECT * FROM Event WHERE JsonData Like '%2019-10-28T16:16:28.8080376-04:00%'");
                if (test.Any())
                    Console.WriteLine("Achou no SQL");
            }

            var endTime = DateTime.Now;

            Console.WriteLine("SQL = " + endTime.Subtract(startTime).TotalMilliseconds + " ms");
            //Console.WriteLine("Terminado o SQL");

            // ----------------------------- Mongo ----------------------------------------
            Configuration.DataProvider = mongoDataProvider;

            //Console.WriteLine(DateTime.Now.ToString());
            i = 0;
            //while (i < 10000)
            //{
            //    using (var audit = AuditScope.Create("Order:Update", () => order, new { Perfil = "Willian", Configuration = "192.168.1.1" }))
            //    {
            //        audit.Event.StartDate = DateTime.Now;
            //        order.Status = EnumStatus.Finish;

            //        audit.Event.CustomFields["ReferenceId"] = 111;
            //        audit.Event.StartDate = DateTime.Now;

            //        audit.Comment("Salvar a ordem1");
            //        audit.Event.EndDate = DateTime.Now;
            //    }
            //    i++;
            //}

            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("Audit");
            var collection = database.GetCollection<EventMongo>("Event");

            startTime = DateTime.Now;

            var query = Builders<EventMongo>.Filter.Regex("StartDate", new BsonRegularExpression("2019-10-28T20:16:33.472+00:00"));
            var list = collection.Find(x => x.StartDate == DateTime.Parse("2019-10-28T20:16:35.568+00:00")).ToList();

            foreach (var document in list)
            {
                Console.WriteLine("Achou no Mongo");
            }
            endTime = DateTime.Now;
            Console.WriteLine("Mongo = " + endTime.Subtract(startTime).TotalMilliseconds + " ms");

            //Console.WriteLine("Terminado o Mongo");
            //Console.WriteLine(DateTime.Now.ToString());

            //AuditScope auditScope = null;
            //try
            //{
            //    auditScope = await AuditScope.CreateAsync("Test:Identity", () => order, new { user = "cash" });
            //    {
            //        order.Status = EnumStatus.Start;
            //    }
            //    auditScope.Comment("01");
            //}
            //finally
            //{
            //    if (auditScope != null)
            //    {
            //        await auditScope.DisposeAsync();
            //    }
            //}

            Console.ReadKey();
        }
    }

    public class Order
    {
        public Order()
        {
            Id = 1;
            Name = "name";
            Status = EnumStatus.Init;
        }

        public Order(int id, string name)
        {
            Id = id;
            Name = name;
            Status = EnumStatus.Init;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public EnumStatus Status { get; set; }
    }

    public enum EnumStatus
    {
        Init = 0,
        Start = 1,
        Finish = 2
    }

    public class Event
    {
        public int _id { get; set; }
        public string JsonData { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class EventMongo
    {
        //[BsonElement("_id")]
        //[BsonSerializer(typeof(BsonStringNumericSerializer))]
        public ObjectId _id { get; set; }
        public string EventType { get; set; }
        public object Environment { get; set; }
        public object Target { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Duration { get; set; }
        public string Perfil { get; set; }
        public string Configuration { get; set; }
        public int ReferenceId { get; set; }
    }

    public class BsonStringNumericSerializer : SerializerBase<string>
    {
        public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.CurrentBsonType;
            switch (bsonType)
            {
                case BsonType.Null:
                    context.Reader.ReadNull();
                    return null;
                case BsonType.String:
                    return context.Reader.ReadString();
                case BsonType.ObjectId:
                    return context.Reader.ReadString();
                case BsonType.Int32:
                    return context.Reader.ReadInt32().ToString(CultureInfo.InvariantCulture);
                default:
                    var message = string.Format($"Custom Cannot deserialize BsonString or BsonInt32 from BsonType {bsonType}");
                    throw new BsonSerializationException(message);
            }
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
        {
            if (value != null)
            {
                if (int.TryParse(value, out var result))
                {
                    context.Writer.WriteInt32(result);
                }
                else
                {
                    context.Writer.WriteString(value);
                }
            }
            else
            {
                context.Writer.WriteNull();
            }
        }
    }
}