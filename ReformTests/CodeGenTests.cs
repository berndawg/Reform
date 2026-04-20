using Microsoft.Data.Sqlite;
using Reform;
using Reform.Interfaces;
using Xunit;

namespace ReformTests;

public class CodeGenTests : IDisposable
{
    private readonly SqliteConnection _sharedConnection;
    private readonly ReformFactory _factory;

    public CodeGenTests()
    {
        _sharedConnection = new SqliteConnection("Data Source=CodeGenTest;Mode=Memory;Cache=Shared");
        _sharedConnection.Open();

        CreateTables(_sharedConnection);

        _factory = new Reformer()
            .UseSqlite("Data Source=CodeGenTest;Mode=Memory;Cache=Shared")
            .Register(typeof(IDebugLogger), typeof(TestDebugLogger))
            .Build();
    }

    public void Dispose()
    {
        _factory.Dispose();
        _sharedConnection.Close();
        _sharedConnection.Dispose();
    }

    private static void CreateTables(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS [Country] (
                [CountryId] INTEGER PRIMARY KEY AUTOINCREMENT,
                [CountryName] TEXT
            );
            CREATE TABLE IF NOT EXISTS [Airport] (
                [AirportId] INTEGER PRIMARY KEY AUTOINCREMENT,
                [AirportCode] TEXT NOT NULL,
                [AirportName] TEXT NOT NULL,
                [CountryId] INTEGER NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void CodeGen_Generates_Class_With_EntityMetadata()
    {
        var result = _factory.CodeGen("Country");

        Assert.Contains("[EntityMetadata(TableName = \"Country\")]", result);
        Assert.Contains("public class Country", result);
        Assert.Contains("using Reform.Attributes;", result);
    }

    [Fact]
    public void CodeGen_Detects_PrimaryKey_And_Identity()
    {
        var result = _factory.CodeGen("Country");

        Assert.Contains("IsPrimaryKey = true", result);
        Assert.Contains("IsIdentity = true", result);
        Assert.Contains("public int CountryId { get; set; }", result);
    }

    [Fact]
    public void CodeGen_Detects_Nullable_Column()
    {
        var result = _factory.CodeGen("Country");

        // CountryName is TEXT with no NOT NULL constraint, so it should be nullable
        Assert.Contains("public string? CountryName { get; set; }", result);
    }

    [Fact]
    public void CodeGen_Detects_Required_NonNullable_Columns()
    {
        var result = _factory.CodeGen("Airport");

        // AirportCode and AirportName are NOT NULL, so should be required
        Assert.Contains("IsRequired = true", result);
        Assert.Contains("public string AirportCode { get; set; } = \"\";", result);
        Assert.Contains("public string AirportName { get; set; } = \"\";", result);
    }

    [Fact]
    public void CodeGen_NonNullable_Int_Is_Required()
    {
        var result = _factory.CodeGen("Airport");

        // CountryId is INTEGER NOT NULL and not a PK, so IsRequired
        Assert.Contains("ColumnName = \"CountryId\"", result);
        // The CountryId line should have IsRequired
        var lines = result.Split('\n');
        var countryIdAttr = Array.Find(lines, l => l.Contains("ColumnName = \"CountryId\""));
        Assert.NotNull(countryIdAttr);
        Assert.Contains("IsRequired = true", countryIdAttr);
    }

    [Fact]
    public void CodeGen_PrimaryKey_Does_Not_Have_IsRequired()
    {
        var result = _factory.CodeGen("Country");

        var lines = result.Split('\n');
        var pkAttr = Array.Find(lines, l => l.Contains("IsPrimaryKey = true"));
        Assert.NotNull(pkAttr);
        Assert.DoesNotContain("IsRequired = true", pkAttr);
    }

    [Fact]
    public void CodeGen_Throws_For_NonExistent_Table()
    {
        Assert.Throws<InvalidOperationException>(() => _factory.CodeGen("NonExistentTable"));
    }

    [Fact]
    public void CodeGen_Via_ICodeGenerator_Interface()
    {
        var codeGen = _factory.Resolve<ICodeGenerator>();
        var result = codeGen.CodeGen("Airport");

        Assert.Contains("public class Airport", result);
        Assert.Contains("[EntityMetadata(TableName = \"Airport\")]", result);
    }
}
