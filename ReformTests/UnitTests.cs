using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Reform.Interfaces;
using Reform.Logic;
using Reform.Objects;
using ReformTests.Objects;
using Xunit;

namespace ReformTests
{
    public class UnitTests : IDisposable
    {
        private readonly SqliteConnection _sharedConnection;

        public UnitTests()
        {
            // Use a shared in-memory database for all tests in this class instance
            _sharedConnection = new SqliteConnection("Data Source=InMemoryTest;Mode=Memory;Cache=Shared");
            _sharedConnection.Open();

            CreateTables(_sharedConnection);

            Reformer.UseSqlite();
            Reformer.RegisterType(typeof(IConnectionStringProvider), typeof(TestConnectionStringProvider));
            Reformer.RegisterType(typeof(IDebugLogger), typeof(TestDebugLogger));
        }

        public void Dispose()
        {
            _sharedConnection.Close();
            _sharedConnection.Dispose();
        }

        private void CreateTables(SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
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
                    DELETE FROM [Airport];
                    DELETE FROM [Country];
                ";
                cmd.ExecuteNonQuery();
            }
        }

        [Fact]
        public void Insert_And_Count()
        {
            IReform<Country> countryLogic = Reformer.Reform<Country>();

            countryLogic.Insert(new Country { CountryName = "Morocco" });
            countryLogic.Insert(new Country { CountryName = "United States" });
            countryLogic.Insert(new Country { CountryName = "Iceland" });

            Assert.Equal(3, countryLogic.Count());
        }

        [Fact]
        public void Count_With_Predicate()
        {
            IReform<Country> countryLogic = Reformer.Reform<Country>();
            IReform<Airport> airportLogic = Reformer.Reform<Airport>();

            countryLogic.Insert(new Country { CountryName = "USA" });
            countryLogic.Insert(new Country { CountryName = "Peru" });

            // Get the IDs assigned by SQLite
            var countries = countryLogic.Select().ToList();
            int usaId = countries.First(c => c.CountryName == "USA").CountryId;
            int peruId = countries.First(c => c.CountryName == "Peru").CountryId;

            airportLogic.Insert(new Airport { AirportCode = "LAX", AirportName = "Los Angeles", CountryId = usaId });
            airportLogic.Insert(new Airport { AirportCode = "AUS", AirportName = "Austin", CountryId = usaId });
            airportLogic.Insert(new Airport { AirportCode = "LIM", AirportName = "Lima", CountryId = peruId });

            Assert.Equal(2, airportLogic.Count(x => x.CountryId == usaId));
            Assert.Equal(1, airportLogic.Count(x => x.CountryId == peruId));
        }

        [Fact]
        public void Exists_Returns_True_When_Found()
        {
            IReform<Airport> airportLogic = Reformer.Reform<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "RAK", AirportName = "Marrakesh", CountryId = 1 });

            Assert.True(airportLogic.Exists(x => x.AirportCode == "RAK"));
        }

        [Fact]
        public void Exists_Returns_False_When_Not_Found()
        {
            IReform<Airport> airportLogic = Reformer.Reform<Airport>();

            Assert.False(airportLogic.Exists(x => x.AirportCode == "NONEXISTENT"));
        }

        [Fact]
        public void Select_All()
        {
            IReform<Country> countryLogic = Reformer.Reform<Country>();

            countryLogic.Insert(new Country { CountryName = "France" });
            countryLogic.Insert(new Country { CountryName = "Germany" });

            var all = countryLogic.Select().ToList();
            Assert.True(all.Count >= 2);
        }

        [Fact]
        public void Select_With_Lambda()
        {
            IReform<Airport> airportLogic = Reformer.Reform<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "CDG", AirportName = "Charles de Gaulle", CountryId = 10 });
            airportLogic.Insert(new Airport { AirportCode = "ORY", AirportName = "Orly", CountryId = 10 });
            airportLogic.Insert(new Airport { AirportCode = "FCO", AirportName = "Fiumicino", CountryId = 20 });

            var frenchAirports = airportLogic.Select(x => x.CountryId == 10).ToList();
            Assert.Equal(2, frenchAirports.Count);
        }

        [Fact]
        public void SelectSingle()
        {
            IReform<Airport> airportLogic = Reformer.Reform<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "NRT", AirportName = "Narita", CountryId = 30 });

            Airport airport = airportLogic.SelectSingle(x => x.AirportCode == "NRT");
            Assert.Equal("Narita", airport.AirportName);
        }

        [Fact]
        public void SelectSingleOrDefault_Returns_Null_When_Not_Found()
        {
            IReform<Airport> airportLogic = Reformer.Reform<Airport>();

            Airport airport = airportLogic.SelectSingleOrDefault(x => x.AirportCode == "ZZZZZ");
            Assert.Null(airport);
        }

        [Fact]
        public void Update_Modifies_Record()
        {
            IReform<Country> countryLogic = Reformer.Reform<Country>();

            var country = new Country { CountryName = "Moroco" }; // typo
            countryLogic.Insert(country);

            country.CountryName = "Morocco";
            countryLogic.Update(country);

            Country updated = countryLogic.SelectSingle(x => x.CountryId == country.CountryId);
            Assert.Equal("Morocco", updated.CountryName);
        }

        [Fact]
        public void Delete_Removes_Record()
        {
            IReform<Airport> airportLogic = Reformer.Reform<Airport>();

            var airport = new Airport { AirportCode = "DEL", AirportName = "Delhi", CountryId = 40 };
            airportLogic.Insert(airport);

            Assert.True(airportLogic.Exists(x => x.AirportCode == "DEL"));

            airportLogic.Delete(airport);

            Assert.False(airportLogic.Exists(x => x.AirportCode == "DEL"));
        }

        [Fact]
        public void Delete_List_Removes_Multiple()
        {
            IReform<Airport> airportLogic = Reformer.Reform<Airport>();

            var a1 = new Airport { AirportCode = "XX1", AirportName = "Test1", CountryId = 50 };
            var a2 = new Airport { AirportCode = "XX2", AirportName = "Test2", CountryId = 50 };
            airportLogic.Insert(a1);
            airportLogic.Insert(a2);

            Assert.True(airportLogic.Exists(x => x.AirportCode == "XX1"));
            Assert.True(airportLogic.Exists(x => x.AirportCode == "XX2"));

            airportLogic.Delete(new List<Airport> { a1, a2 });

            Assert.False(airportLogic.Exists(x => x.AirportCode == "XX1"));
            Assert.False(airportLogic.Exists(x => x.AirportCode == "XX2"));
        }

        [Fact]
        public void Insert_List_Inserts_Multiple()
        {
            IReform<Country> countryLogic = Reformer.Reform<Country>();

            int before = countryLogic.Count();

            countryLogic.Insert(new List<Country>
            {
                new Country { CountryName = "Japan" },
                new Country { CountryName = "Brazil" }
            });

            int after = countryLogic.Count();
            Assert.Equal(before + 2, after);
        }

        [Fact]
        public void Select_With_And_Predicate()
        {
            IReform<Airport> airportLogic = Reformer.Reform<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "JFK", AirportName = "John F Kennedy", CountryId = 100 });
            airportLogic.Insert(new Airport { AirportCode = "EWR", AirportName = "Newark", CountryId = 100 });

            var result = airportLogic.Select(x => x.CountryId == 100 && x.AirportCode == "JFK").ToList();
            Assert.Single(result);
            Assert.Equal("John F Kennedy", result[0].AirportName);
        }

        [Fact]
        public void Select_With_Or_Predicate()
        {
            IReform<Airport> airportLogic = Reformer.Reform<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "SYD", AirportName = "Sydney", CountryId = 200 });
            airportLogic.Insert(new Airport { AirportCode = "MEL", AirportName = "Melbourne", CountryId = 200 });
            airportLogic.Insert(new Airport { AirportCode = "BNE", AirportName = "Brisbane", CountryId = 200 });

            var result = airportLogic.Select(x => x.AirportCode == "SYD" || x.AirportCode == "MEL").ToList();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Validation_Throws_On_Missing_Required_Field()
        {
            IReform<Airport> airportLogic = Reformer.Reform<Airport>();

            var airport = new Airport { AirportCode = "", AirportName = "Test", CountryId = 1 };

            Assert.Throws<ApplicationException>(() => airportLogic.Insert(airport));
        }
    }

    internal class TestConnectionStringProvider : IConnectionStringProvider
    {
        public string GetConnectionString(string databaseName)
        {
            return "Data Source=InMemoryTest;Mode=Memory;Cache=Shared";
        }
    }

    internal class TestDebugLogger : IDebugLogger
    {
        public void WriteLine(string stringValue)
        {
            Console.WriteLine(stringValue);
        }
    }
}
