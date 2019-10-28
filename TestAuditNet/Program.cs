using System;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;
using Audit.MongoDB.Providers;
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

            Configuration.DataProvider = mongoDataProvider;
            
            //Configuration.Setup().UseMongoDB(config => config.ConnectionString("mongodb://localhost:27017")
            //    .Database("Teste")
            //    .Collection("Event"));
            Console.WriteLine("Start");

            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("Teste");
            var collection = database.GetCollection<Order>("Event");

            //var order1 = new Order(4, "Jack");
            //collection.InsertOne(order1);

            //var mongoDataProvider = MongoDataProvider;
            
            var test = mongoDataProvider.QueryEvents()
                .Where(ev => ev.Comments.Contains("Salvar a ordem2"))
                .OrderByDescending(ev => ev.Duration)
                .Take(10).ToList();

            //var list = collection.Find(order1).ToList();

            //foreach (var document in list)
            //{
            //    Console.WriteLine(document["Name"]);
            //}

            var order = new Order
            {
                Status = EnumStatus.Start
            };

            using (var audit = AuditScope.Create("Order:Update", () => order, new { UserName = "Willian" }))
            {
                Console.WriteLine("Audit");
                order.Status = EnumStatus.Finish;

                audit.Event.CustomFields["ReferenceId"] = 111;

                audit.Comment("Salvar a ordem1");
                audit.Comment("Salvar a ordem2");
            }

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
}