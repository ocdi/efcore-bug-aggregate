// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

Console.WriteLine("Hello, World!");


await using var server = new MsSqlBuilder().Build();

await server.StartAsync();

var options = new DbContextOptionsBuilder<TestContext>().UseSqlServer(server.GetConnectionString()).Options;

var db = new TestContext(options);
await db.Database.EnsureCreatedAsync();



var query = db.Set<Receipt>()
.GroupBy(a => new { OwnerId = a.OwnerId, a.OwnerType })
.Select(a => new { a.Key, Total = a.Sum(t => t.Lines.Sum(s => s.Amount)) });

//"SELECT [r].[OwnerId], [r].[OwnerType], COALESCE(SUM((\r\n    SELECT COALESCE(SUM([l].[Amount]), 0)\r\n    FROM [Lines] AS [l]\r\n    WHERE [r].[Id] = [l].[ReceiptId1])), 0) AS [Total]\r\nFROM [Receipts] AS [r]\r\nGROUP BY [r].[OwnerId], [r].[OwnerType]"

var r = query.ToArray();


var queryAlt = db.Set<Receipt>()
            .Select(a => new { OwnerId = a.OwnerId, a.OwnerType, Cents = a.Lines.Sum(a => a.Amount) })
            .GroupBy(a => new { OwnerId = a.OwnerId, a.OwnerType })
            .Select(a => new { a.Key, Total = a.Sum(t => t.Cents) });
var rAlt = queryAlt.ToArray();

// "SELECT [r].[OwnerId], [r].[OwnerType], COALESCE(SUM((\r\n    SELECT COALESCE(SUM([l].[Amount]), 0)\r\n    FROM [Lines] AS [l]\r\n    WHERE [r].[Id] = [l].[ReceiptId1])), 0) AS [Total]\r\nFROM [Receipts] AS [r]\r\nGROUP BY [r].[OwnerId], [r].[OwnerType]"
// Cannot perform an aggregate function on an expression containing an aggregate or a subquery.


Console.WriteLine(r.Length);

public class Receipt
{
    public string Id { get; set; }
    public string OwnerId { get; set; }
    public string OwnerType { get; set; }
    public ICollection<Lines> Lines { get; set; }
}

public class Lines
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public int Amount { get; set; }
}

public class TestContext : DbContext
{
    public DbSet<Receipt> Receipts { get; set; }
    public TestContext(DbContextOptions options) : base(options) { }
}