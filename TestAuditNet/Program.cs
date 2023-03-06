using Audit.Core;
using Audit.MongoDB.Providers;
using Audit.SqlServer;
using Audit.SqlServer.Providers;
using Dapper;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace TestAuditNet;

internal class Program
{
    private static void Main(string[] args)
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

        //var i = 0;
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
        using (var conexao =
               new SqlConnection("data source=localhost;initial catalog=Auditoria;integrated security=true;"))
        {
            var test = conexao.Query<dynamic>(
                "SELECT * FROM Event WHERE JsonData Like '%2019-10-28T16:16:28.8080376-04:00%'");
            if (test.Any())
                Console.WriteLine("Achou no SQL");
        }

        var endTime = DateTime.Now;

        Console.WriteLine("SQL = " + endTime.Subtract(startTime).TotalMilliseconds + " ms");

        // ----------------------------- Mongo ----------------------------------------
        Configuration.DataProvider = mongoDataProvider;

        //i = 0;
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

        var query = Builders<EventMongo>.Filter.Eq("StartDate", "2019-10-28T20:16:35.568+00:00");
        var list = collection.Find(query).ToList();

        foreach (var document in list)
        {
            Console.WriteLine("Achou no Mongo");
        }

        endTime = DateTime.Now;
        Console.WriteLine("Mongo = " + endTime.Subtract(startTime).TotalMilliseconds + " ms");

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