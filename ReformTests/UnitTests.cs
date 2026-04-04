using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Reform;
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
        private readonly ReformFactory _reform;

        public UnitTests()
        {
            _sharedConnection = new SqliteConnection("Data Source=InMemoryTest;Mode=Memory;Cache=Shared");
            _sharedConnection.Open();

            CreateTables(_sharedConnection);

            _reform = new ReformBuilder()
                .UseSqlite("Data Source=InMemoryTest;Mode=Memory;Cache=Shared")
                .Register(typeof(IDebugLogger), typeof(TestDebugLogger))
                .Build();
        }

        public void Dispose()
        {
            _reform.Dispose();
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

        #region Sync Tests

        [Fact]
        public void Insert_And_Count()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            countryLogic.Insert(new Country { CountryName = "Morocco" });
            countryLogic.Insert(new Country { CountryName = "United States" });
            countryLogic.Insert(new Country { CountryName = "Iceland" });

            Assert.Equal(3, countryLogic.Count());
        }

        [Fact]
        public void Count_With_Predicate()
        {
            IReform<Country> countryLogic = _reform.For<Country>();
            IReform<Airport> airportLogic = _reform.For<Airport>();

            countryLogic.Insert(new Country { CountryName = "USA" });
            countryLogic.Insert(new Country { CountryName = "Peru" });

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
            IReform<Airport> airportLogic = _reform.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "RAK", AirportName = "Marrakesh", CountryId = 1 });

            Assert.True(airportLogic.Exists(x => x.AirportCode == "RAK"));
        }

        [Fact]
        public void Exists_Returns_False_When_Not_Found()
        {
            IReform<Airport> airportLogic = _reform.For<Airport>();

            Assert.False(airportLogic.Exists(x => x.AirportCode == "NONEXISTENT"));
        }

        [Fact]
        public void Select_All()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            countryLogic.Insert(new Country { CountryName = "France" });
            countryLogic.Insert(new Country { CountryName = "Germany" });

            var all = countryLogic.Select().ToList();
            Assert.True(all.Count >= 2);
        }

        [Fact]
        public void Select_With_Lambda()
        {
            IReform<Airport> airportLogic = _reform.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "CDG", AirportName = "Charles de Gaulle", CountryId = 10 });
            airportLogic.Insert(new Airport { AirportCode = "ORY", AirportName = "Orly", CountryId = 10 });
            airportLogic.Insert(new Airport { AirportCode = "FCO", AirportName = "Fiumicino", CountryId = 20 });

            var frenchAirports = airportLogic.Select(x => x.CountryId == 10).ToList();
            Assert.Equal(2, frenchAirports.Count);
        }

        [Fact]
        public void SelectSingle()
        {
            IReform<Airport> airportLogic = _reform.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "NRT", AirportName = "Narita", CountryId = 30 });

            Airport airport = airportLogic.SelectSingle(x => x.AirportCode == "NRT");
            Assert.Equal("Narita", airport.AirportName);
        }

        [Fact]
        public void SelectSingleOrDefault_Returns_Null_When_Not_Found()
        {
            IReform<Airport> airportLogic = _reform.For<Airport>();

            Airport airport = airportLogic.SelectSingleOrDefault(x => x.AirportCode == "ZZZZZ");
            Assert.Null(airport);
        }

        [Fact]
        public void Update_Modifies_Record()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            var country = new Country { CountryName = "Moroco" };
            countryLogic.Insert(country);

            country.CountryName = "Morocco";
            countryLogic.Update(country);

            Country updated = countryLogic.SelectSingle(x => x.CountryId == country.CountryId);
            Assert.Equal("Morocco", updated.CountryName);
        }

        [Fact]
        public void Delete_Removes_Record()
        {
            IReform<Airport> airportLogic = _reform.For<Airport>();

            var airport = new Airport { AirportCode = "DEL", AirportName = "Delhi", CountryId = 40 };
            airportLogic.Insert(airport);

            Assert.True(airportLogic.Exists(x => x.AirportCode == "DEL"));

            airportLogic.Delete(airport);

            Assert.False(airportLogic.Exists(x => x.AirportCode == "DEL"));
        }

        [Fact]
        public void Delete_List_Removes_Multiple()
        {
            IReform<Airport> airportLogic = _reform.For<Airport>();

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
            IReform<Country> countryLogic = _reform.For<Country>();

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
            IReform<Airport> airportLogic = _reform.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "JFK", AirportName = "John F Kennedy", CountryId = 100 });
            airportLogic.Insert(new Airport { AirportCode = "EWR", AirportName = "Newark", CountryId = 100 });

            var result = airportLogic.Select(x => x.CountryId == 100 && x.AirportCode == "JFK").ToList();
            Assert.Single(result);
            Assert.Equal("John F Kennedy", result[0].AirportName);
        }

        [Fact]
        public void Select_With_Or_Predicate()
        {
            IReform<Airport> airportLogic = _reform.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "SYD", AirportName = "Sydney", CountryId = 200 });
            airportLogic.Insert(new Airport { AirportCode = "MEL", AirportName = "Melbourne", CountryId = 200 });
            airportLogic.Insert(new Airport { AirportCode = "BNE", AirportName = "Brisbane", CountryId = 200 });

            var result = airportLogic.Select(x => x.AirportCode == "SYD" || x.AirportCode == "MEL").ToList();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Validation_Throws_On_Missing_Required_Field()
        {
            IReform<Airport> airportLogic = _reform.For<Airport>();

            var airport = new Airport { AirportCode = "", AirportName = "Test", CountryId = 1 };

            Assert.Throws<ApplicationException>(() => airportLogic.Insert(airport));
        }

        #endregion

        #region Async Tests

        [Fact]
        public async Task Insert_And_Count_Async()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            await countryLogic.InsertAsync(new Country { CountryName = "Argentina" });
            await countryLogic.InsertAsync(new Country { CountryName = "Chile" });

            Assert.Equal(2, await countryLogic.CountAsync());
        }

        [Fact]
        public async Task Select_Async()
        {
            IReform<Airport> airportLogic = _reform.For<Airport>();

            await airportLogic.InsertAsync(new Airport { AirportCode = "GRU", AirportName = "Guarulhos", CountryId = 300 });
            await airportLogic.InsertAsync(new Airport { AirportCode = "GIG", AirportName = "Galeao", CountryId = 300 });

            var airports = (await airportLogic.SelectAsync(x => x.CountryId == 300)).ToList();
            Assert.Equal(2, airports.Count);
        }

        [Fact]
        public async Task Update_Async()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            var country = new Country { CountryName = "Barzil" };
            await countryLogic.InsertAsync(country);

            country.CountryName = "Brazil";
            await countryLogic.UpdateAsync(country);

            Country updated = await countryLogic.SelectSingleAsync(x => x.CountryId == country.CountryId);
            Assert.Equal("Brazil", updated.CountryName);
        }

        [Fact]
        public async Task Delete_Async()
        {
            IReform<Airport> airportLogic = _reform.For<Airport>();

            var airport = new Airport { AirportCode = "EZE", AirportName = "Ezeiza", CountryId = 400 };
            await airportLogic.InsertAsync(airport);

            Assert.True(await airportLogic.ExistsAsync(x => x.AirportCode == "EZE"));

            await airportLogic.DeleteAsync(airport);

            Assert.False(await airportLogic.ExistsAsync(x => x.AirportCode == "EZE"));
        }

        [Fact]
        public async Task SelectSingleOrDefault_Async_Returns_Null()
        {
            IReform<Airport> airportLogic = _reform.For<Airport>();

            Airport airport = await airportLogic.SelectSingleOrDefaultAsync(x => x.AirportCode == "NOPE");
            Assert.Null(airport);
        }

        [Fact]
        public async Task Exists_Async_Returns_False_When_Not_Found()
        {
            IReform<Airport> airportLogic = _reform.For<Airport>();

            Assert.False(await airportLogic.ExistsAsync(x => x.AirportCode == "NONEXISTENT"));
        }

        #endregion

        #region Merge Tests

        [Fact]
        public void Merge_Inserts_New_Items()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            countryLogic.Merge(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" }
            });

            Assert.Equal(2, countryLogic.Count());
            Assert.True(countryLogic.Exists(x => x.CountryName == "France"));
            Assert.True(countryLogic.Exists(x => x.CountryName == "Germany"));
        }

        [Fact]
        public void Merge_Updates_Existing_Items()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            countryLogic.Insert(new Country { CountryName = "France" });
            Country france = countryLogic.SelectSingle(x => x.CountryName == "France");

            france.CountryName = "French Republic";
            countryLogic.Merge(new List<Country> { france });

            Assert.Equal(1, countryLogic.Count());
            Country updated = countryLogic.SelectSingle(x => x.CountryId == france.CountryId);
            Assert.Equal("French Republic", updated.CountryName);
        }

        [Fact]
        public void Merge_Deletes_Missing_Items()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            countryLogic.Insert(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" },
                new Country { CountryName = "Spain" }
            });

            Country france = countryLogic.SelectSingle(x => x.CountryName == "France");

            countryLogic.Merge(new List<Country> { france });

            Assert.Equal(1, countryLogic.Count());
            Assert.True(countryLogic.Exists(x => x.CountryName == "France"));
            Assert.False(countryLogic.Exists(x => x.CountryName == "Germany"));
            Assert.False(countryLogic.Exists(x => x.CountryName == "Spain"));
        }

        [Fact]
        public void Merge_Full_Reconciliation()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            countryLogic.Insert(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" },
                new Country { CountryName = "Spain" }
            });

            Country france = countryLogic.SelectSingle(x => x.CountryName == "France");
            france.CountryName = "French Republic";

            Country germany = countryLogic.SelectSingle(x => x.CountryName == "Germany");

            countryLogic.Merge(new List<Country>
            {
                france,                                    // update
                germany,                                   // unchanged
                new Country { CountryName = "Italy" }      // insert
            });
            // Spain should be deleted

            Assert.Equal(3, countryLogic.Count());
            Assert.True(countryLogic.Exists(x => x.CountryName == "French Republic"));
            Assert.True(countryLogic.Exists(x => x.CountryName == "Germany"));
            Assert.True(countryLogic.Exists(x => x.CountryName == "Italy"));
            Assert.False(countryLogic.Exists(x => x.CountryName == "Spain"));
            Assert.False(countryLogic.Exists(x => x.CountryName == "France"));
        }

        [Fact]
        public void Merge_Empty_List_Deletes_All()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            countryLogic.Insert(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" }
            });

            Assert.Equal(2, countryLogic.Count());

            countryLogic.Merge(new List<Country>());

            Assert.Equal(0, countryLogic.Count());
        }

        [Fact]
        public void Merge_With_No_Changes()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            countryLogic.Insert(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" }
            });

            var all = countryLogic.Select().ToList();

            countryLogic.Merge(all);

            Assert.Equal(2, countryLogic.Count());
            Assert.True(countryLogic.Exists(x => x.CountryName == "France"));
            Assert.True(countryLogic.Exists(x => x.CountryName == "Germany"));
        }

        [Fact]
        public async Task Merge_Async()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            await countryLogic.InsertAsync(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" },
                new Country { CountryName = "Spain" }
            });

            Country france = await countryLogic.SelectSingleAsync(x => x.CountryName == "France");
            france.CountryName = "French Republic";

            await countryLogic.MergeAsync(new List<Country>
            {
                france,
                new Country { CountryName = "Italy" }
            });

            Assert.Equal(2, await countryLogic.CountAsync());
            Assert.True(await countryLogic.ExistsAsync(x => x.CountryName == "French Republic"));
            Assert.True(await countryLogic.ExistsAsync(x => x.CountryName == "Italy"));
            Assert.False(await countryLogic.ExistsAsync(x => x.CountryName == "Germany"));
            Assert.False(await countryLogic.ExistsAsync(x => x.CountryName == "Spain"));
        }

        #endregion

        #region Transaction Tests

        [Fact]
        public void Transaction_Insert_Update_Delete()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            using (var connection = countryLogic.GetConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    countryLogic.Insert(connection, transaction, new Country { CountryName = "Norway" });
                    countryLogic.Insert(connection, transaction, new Country { CountryName = "Sweden" });
                    transaction.Commit();
                }
            }

            Assert.Equal(2, countryLogic.Count());

            var norway = countryLogic.SelectSingle(x => x.CountryName == "Norway");

            using (var connection = countryLogic.GetConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    norway.CountryName = "Kingdom of Norway";
                    countryLogic.Update(connection, transaction, norway);
                    transaction.Commit();
                }
            }

            Assert.True(countryLogic.Exists(x => x.CountryName == "Kingdom of Norway"));

            using (var connection = countryLogic.GetConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    countryLogic.Delete(connection, transaction, norway);
                    transaction.Commit();
                }
            }

            Assert.Equal(1, countryLogic.Count());
        }

        [Fact]
        public void Transaction_Rollback()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            using (var connection = countryLogic.GetConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    countryLogic.Insert(connection, transaction, new Country { CountryName = "Japan" });
                    countryLogic.Insert(connection, transaction, new Country { CountryName = "China" });
                    transaction.Rollback();
                }
            }

            Assert.Equal(0, countryLogic.Count());
        }

        [Fact]
        public async Task Transaction_Async_Insert_Update_Delete()
        {
            IReform<Country> countryLogic = _reform.For<Country>();

            using (var connection = countryLogic.GetConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    await countryLogic.InsertAsync(connection, transaction, new Country { CountryName = "Brazil" });
                    await countryLogic.InsertAsync(connection, transaction, new Country { CountryName = "Argentina" });
                    transaction.Commit();
                }
            }

            Assert.Equal(2, await countryLogic.CountAsync());

            var brazil = countryLogic.SelectSingle(x => x.CountryName == "Brazil");

            using (var connection = countryLogic.GetConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    brazil.CountryName = "Federative Republic of Brazil";
                    await countryLogic.UpdateAsync(connection, transaction, brazil);
                    transaction.Commit();
                }
            }

            Assert.True(await countryLogic.ExistsAsync(x => x.CountryName == "Federative Republic of Brazil"));

            using (var connection = countryLogic.GetConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    await countryLogic.DeleteAsync(connection, transaction, brazil);
                    transaction.Commit();
                }
            }

            Assert.Equal(1, await countryLogic.CountAsync());
        }

        #endregion
    }

    internal class TestDebugLogger : IDebugLogger
    {
        public void WriteLine(string stringValue)
        {
            Console.WriteLine(stringValue);
        }
    }
}
