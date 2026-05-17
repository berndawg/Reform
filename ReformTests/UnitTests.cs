using Microsoft.Data.Sqlite;
using Reform;
using Reform.Enum;
using Reform.Interfaces;
using Reform.Objects;
using ReformTests.Objects;
using Xunit;

namespace ReformTests
{
    public class UnitTests : IDisposable
    {
        private readonly SqliteConnection _sharedConnection;
        private readonly ReformFactory _reformer;

        public UnitTests()
        {
            _sharedConnection = new SqliteConnection("Data Source=InMemoryTest;Mode=Memory;Cache=Shared");
            _sharedConnection.Open();

            CreateTables(_sharedConnection);

            _reformer = new Reformer()
                .UseSqlite("Data Source=InMemoryTest;Mode=Memory;Cache=Shared")
                .Register(typeof(IDebugLogger), typeof(TestDebugLogger))
                .Build();
        }

        public void Dispose()
        {
            _reformer.Dispose();
            _sharedConnection.Close();
            _sharedConnection.Dispose();
        }

        private void CreateTables(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();
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

        #region Sync Tests

        [Fact]
        public void Insert_And_Count()
        {
            var countryLogic = _reformer.For<Country>();

            countryLogic.Insert(new Country { CountryName = "Morocco" });
            countryLogic.Insert(new Country { CountryName = "United States" });
            countryLogic.Insert(new Country { CountryName = "Iceland" });

            Assert.Equal(3, countryLogic.Count());
        }

        [Fact]
        public void Count_With_Predicate()
        {
            var countryLogic = _reformer.For<Country>();
            var airportLogic = _reformer.For<Airport>();

            countryLogic.Insert(new Country { CountryName = "USA" });
            countryLogic.Insert(new Country { CountryName = "Peru" });

            var countries = countryLogic.Select().ToList();
            var usaId = countries.First(c => c.CountryName == "USA").CountryId;
            var peruId = countries.First(c => c.CountryName == "Peru").CountryId;

            airportLogic.Insert(new Airport { AirportCode = "LAX", AirportName = "Los Angeles", CountryId = usaId });
            airportLogic.Insert(new Airport { AirportCode = "AUS", AirportName = "Austin", CountryId = usaId });
            airportLogic.Insert(new Airport { AirportCode = "LIM", AirportName = "Lima", CountryId = peruId });

            Assert.Equal(2, airportLogic.Count(x => x.CountryId == usaId));
            Assert.Equal(1, airportLogic.Count(x => x.CountryId == peruId));
        }

        [Fact]
        public void Exists_Returns_True_When_Found()
        {
            var airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "RAK", AirportName = "Marrakesh", CountryId = 1 });

            Assert.True(airportLogic.Exists(x => x.AirportCode == "RAK"));
        }

        [Fact]
        public void Exists_Returns_False_When_Not_Found()
        {
            var airportLogic = _reformer.For<Airport>();

            Assert.False(airportLogic.Exists(x => x.AirportCode == "NONEXISTENT"));
        }

        [Fact]
        public void Select_All()
        {
            var countryLogic = _reformer.For<Country>();

            countryLogic.Insert(new Country { CountryName = "France" });
            countryLogic.Insert(new Country { CountryName = "Germany" });

            var all = countryLogic.Select().ToList();
            Assert.True(all.Count >= 2);
        }

        [Fact]
        public void Select_With_Lambda()
        {
            var airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "CDG", AirportName = "Charles de Gaulle", CountryId = 10 });
            airportLogic.Insert(new Airport { AirportCode = "ORY", AirportName = "Orly", CountryId = 10 });
            airportLogic.Insert(new Airport { AirportCode = "FCO", AirportName = "Fiumicino", CountryId = 20 });

            var frenchAirports = airportLogic.Select(x => x.CountryId == 10).ToList();
            Assert.Equal(2, frenchAirports.Count);
        }

        [Fact]
        public void SelectSingle()
        {
            var airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "NRT", AirportName = "Narita", CountryId = 30 });

            var airport = airportLogic.SelectSingle(x => x.AirportCode == "NRT");
            Assert.Equal("Narita", airport.AirportName);
        }

        [Fact]
        public void SelectSingleOrDefault_Returns_Null_When_Not_Found()
        {
            var airportLogic = _reformer.For<Airport>();

            var airport = airportLogic.SelectSingleOrDefault(x => x.AirportCode == "ZZZZZ");
            Assert.Null(airport);
        }

        [Fact]
        public void Update_Modifies_Record()
        {
            var countryLogic = _reformer.For<Country>();

            var country = new Country { CountryName = "Moroco" };
            countryLogic.Insert(country);

            country.CountryName = "Morocco";
            countryLogic.Update(country);

            var updated = countryLogic.SelectSingle(x => x.CountryId == country.CountryId);
            Assert.Equal("Morocco", updated.CountryName);
        }

        [Fact]
        public void Update_No_Changes_Is_Noop()
        {
            var countryLogic = _reformer.For<Country>();

            var country = new Country { CountryName = "France" };
            countryLogic.Insert(country);

            countryLogic.Update(country);

            var result = countryLogic.SelectSingle(x => x.CountryId == country.CountryId);
            Assert.Equal("France", result.CountryName);
        }

        [Fact]
        public void Delete_Removes_Record()
        {
            var airportLogic = _reformer.For<Airport>();

            var airport = new Airport { AirportCode = "DEL", AirportName = "Delhi", CountryId = 40 };
            airportLogic.Insert(airport);

            Assert.True(airportLogic.Exists(x => x.AirportCode == "DEL"));

            airportLogic.Delete(airport);

            Assert.False(airportLogic.Exists(x => x.AirportCode == "DEL"));
        }

        [Fact]
        public void Delete_List_Removes_Multiple()
        {
            var airportLogic = _reformer.For<Airport>();

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
            var countryLogic = _reformer.For<Country>();

            var before = countryLogic.Count();

            countryLogic.Insert(new List<Country>
            {
                new Country { CountryName = "Japan" },
                new Country { CountryName = "Brazil" }
            });

            var after = countryLogic.Count();
            Assert.Equal(before + 2, after);
        }

        [Fact]
        public void Select_With_And_Predicate()
        {
            var airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "JFK", AirportName = "John F Kennedy", CountryId = 100 });
            airportLogic.Insert(new Airport { AirportCode = "EWR", AirportName = "Newark", CountryId = 100 });

            var result = airportLogic.Select(x => x.CountryId == 100 && x.AirportCode == "JFK").ToList();
            Assert.Single(result);
            Assert.Equal("John F Kennedy", result[0].AirportName);
        }

        [Fact]
        public void Select_With_Or_Predicate()
        {
            var airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "SYD", AirportName = "Sydney", CountryId = 200 });
            airportLogic.Insert(new Airport { AirportCode = "MEL", AirportName = "Melbourne", CountryId = 200 });
            airportLogic.Insert(new Airport { AirportCode = "BNE", AirportName = "Brisbane", CountryId = 200 });

            var result = airportLogic.Select(x => x.AirportCode == "SYD" || x.AirportCode == "MEL").ToList();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Validation_Throws_On_Missing_Required_Field()
        {
            var airportLogic = _reformer.For<Airport>();

            var airport = new Airport { AirportCode = "", AirportName = "Test", CountryId = 1 };

            Assert.Throws<ArgumentException>(() => airportLogic.Insert(airport));
        }

        [Fact]
        public void Validation_Throws_On_Default_Required_Int()
        {
            var airportLogic = _reformer.For<Airport>();

            var airport = new Airport { AirportCode = "TST", AirportName = "Test", CountryId = 0 };

            Assert.Throws<ArgumentException>(() => airportLogic.Insert(airport));
        }

        #endregion

        #region Async Tests

        [Fact]
        public async Task Insert_And_Count_Async()
        {
            var countryLogic = _reformer.For<Country>();

            await countryLogic.InsertAsync(new Country { CountryName = "Argentina" });
            await countryLogic.InsertAsync(new Country { CountryName = "Chile" });

            Assert.Equal(2, await countryLogic.CountAsync());
        }

        [Fact]
        public async Task Select_Async()
        {
            var airportLogic = _reformer.For<Airport>();

            await airportLogic.InsertAsync(new Airport { AirportCode = "GRU", AirportName = "Guarulhos", CountryId = 300 });
            await airportLogic.InsertAsync(new Airport { AirportCode = "GIG", AirportName = "Galeao", CountryId = 300 });

            var airports = (await airportLogic.SelectAsync(x => x.CountryId == 300)).ToList();
            Assert.Equal(2, airports.Count);
        }

        [Fact]
        public async Task Update_Async()
        {
            var countryLogic = _reformer.For<Country>();

            var country = new Country { CountryName = "Barzil" };
            await countryLogic.InsertAsync(country);

            country.CountryName = "Brazil";
            await countryLogic.UpdateAsync(country);

            var updated = await countryLogic.SelectSingleAsync(x => x.CountryId == country.CountryId);
            Assert.Equal("Brazil", updated.CountryName);
        }

        [Fact]
        public async Task Delete_Async()
        {
            var airportLogic = _reformer.For<Airport>();

            var airport = new Airport { AirportCode = "EZE", AirportName = "Ezeiza", CountryId = 400 };
            await airportLogic.InsertAsync(airport);

            Assert.True(await airportLogic.ExistsAsync(x => x.AirportCode == "EZE"));

            await airportLogic.DeleteAsync(airport);

            Assert.False(await airportLogic.ExistsAsync(x => x.AirportCode == "EZE"));
        }

        [Fact]
        public async Task SelectSingleOrDefault_Async_Returns_Null()
        {
            var airportLogic = _reformer.For<Airport>();

            var airport = await airportLogic.SelectSingleOrDefaultAsync(x => x.AirportCode == "NOPE");
            Assert.Null(airport);
        }

        [Fact]
        public async Task Exists_Async_Returns_False_When_Not_Found()
        {
            var airportLogic = _reformer.For<Airport>();

            Assert.False(await airportLogic.ExistsAsync(x => x.AirportCode == "NONEXISTENT"));
        }

        #endregion

        #region Merge Tests

        [Fact]
        public void Merge_Inserts_New_Items()
        {
            var countryLogic = _reformer.For<Country>();

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
            var countryLogic = _reformer.For<Country>();

            countryLogic.Insert(new Country { CountryName = "France" });
            var france = countryLogic.SelectSingle(x => x.CountryName == "France");

            france.CountryName = "French Republic";
            countryLogic.Merge(new List<Country> { france });

            Assert.Equal(1, countryLogic.Count());
            var updated = countryLogic.SelectSingle(x => x.CountryId == france.CountryId);
            Assert.Equal("French Republic", updated.CountryName);
        }

        [Fact]
        public void Merge_Deletes_Missing_Items()
        {
            var countryLogic = _reformer.For<Country>();

            countryLogic.Insert(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" },
                new Country { CountryName = "Spain" }
            });

            var france = countryLogic.SelectSingle(x => x.CountryName == "France");

            countryLogic.Merge(new List<Country> { france });

            Assert.Equal(1, countryLogic.Count());
            Assert.True(countryLogic.Exists(x => x.CountryName == "France"));
            Assert.False(countryLogic.Exists(x => x.CountryName == "Germany"));
            Assert.False(countryLogic.Exists(x => x.CountryName == "Spain"));
        }

        [Fact]
        public void Merge_Full_Reconciliation()
        {
            var countryLogic = _reformer.For<Country>();

            countryLogic.Insert(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" },
                new Country { CountryName = "Spain" }
            });

            var france = countryLogic.SelectSingle(x => x.CountryName == "France");
            france.CountryName = "French Republic";

            var germany = countryLogic.SelectSingle(x => x.CountryName == "Germany");

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
        public void Merge_Empty_List_Throws()
        {
            var countryLogic = _reformer.For<Country>();

            countryLogic.Insert(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" }
            });

            Assert.Throws<ArgumentException>(() => countryLogic.Merge(new List<Country>()));

            Assert.Equal(2, countryLogic.Count());
        }

        [Fact]
        public void Merge_With_No_Changes()
        {
            var countryLogic = _reformer.For<Country>();

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
            var countryLogic = _reformer.For<Country>();

            await countryLogic.InsertAsync(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" },
                new Country { CountryName = "Spain" }
            });

            var france = await countryLogic.SelectSingleAsync(x => x.CountryName == "France");
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

        #region Truncate Tests

        [Fact]
        public void Truncate_Removes_All_Rows()
        {
            var countryLogic = _reformer.For<Country>();

            countryLogic.Insert(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" }
            });

            Assert.Equal(2, countryLogic.Count());

            countryLogic.Truncate();

            Assert.Equal(0, countryLogic.Count());
        }

        [Fact]
        public async Task Truncate_Async()
        {
            var countryLogic = _reformer.For<Country>();

            await countryLogic.InsertAsync(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" }
            });

            Assert.Equal(2, await countryLogic.CountAsync());

            await countryLogic.TruncateAsync();

            Assert.Equal(0, await countryLogic.CountAsync());
        }

        #endregion

        #region Complex Predicate Tests

        [Fact]
        public void Select_With_NotEqual()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "AAA", AirportName = "Alpha", CountryId = 500 });
            airportLogic.Insert(new Airport { AirportCode = "BBB", AirportName = "Bravo", CountryId = 500 });
            airportLogic.Insert(new Airport { AirportCode = "CCC", AirportName = "Charlie", CountryId = 500 });

            var result = airportLogic.Select(x => x.CountryId == 500 && x.AirportCode != "BBB").ToList();
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, a => a.AirportCode == "BBB");
        }

        [Fact]
        public void Select_With_GreaterThan()
        {
            IReform<Country> countryLogic = _reformer.For<Country>();

            countryLogic.Insert(new Country { CountryName = "GT_A" });
            countryLogic.Insert(new Country { CountryName = "GT_B" });
            countryLogic.Insert(new Country { CountryName = "GT_C" });

            var all = countryLogic.Select(x => x.CountryName == "GT_A"
                                             || x.CountryName == "GT_B"
                                             || x.CountryName == "GT_C").ToList();
            int midId = all.OrderBy(c => c.CountryId).ElementAt(1).CountryId;

            var result = countryLogic.Select(x => x.CountryId > midId).ToList();
            Assert.All(result, c => Assert.True(c.CountryId > midId));
        }

        [Fact]
        public void Select_With_LessThan()
        {
            IReform<Country> countryLogic = _reformer.For<Country>();

            countryLogic.Insert(new Country { CountryName = "LT_A" });
            countryLogic.Insert(new Country { CountryName = "LT_B" });
            countryLogic.Insert(new Country { CountryName = "LT_C" });

            var all = countryLogic.Select(x => x.CountryName == "LT_A"
                                             || x.CountryName == "LT_B"
                                             || x.CountryName == "LT_C").ToList();
            int maxId = all.Max(c => c.CountryId);

            var result = countryLogic.Select(x => x.CountryId < maxId
                                                && (x.CountryName == "LT_A"
                                                 || x.CountryName == "LT_B"
                                                 || x.CountryName == "LT_C")).ToList();
            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.True(c.CountryId < maxId));
        }

        [Fact]
        public void Select_With_GreaterThanOrEqual()
        {
            IReform<Country> countryLogic = _reformer.For<Country>();

            countryLogic.Insert(new Country { CountryName = "GTE_A" });
            countryLogic.Insert(new Country { CountryName = "GTE_B" });

            var all = countryLogic.Select(x => x.CountryName == "GTE_A"
                                             || x.CountryName == "GTE_B").ToList();
            int minId = all.Min(c => c.CountryId);

            var result = countryLogic.Select(x => x.CountryId >= minId
                                                && (x.CountryName == "GTE_A"
                                                 || x.CountryName == "GTE_B")).ToList();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Select_With_LessThanOrEqual()
        {
            IReform<Country> countryLogic = _reformer.For<Country>();

            countryLogic.Insert(new Country { CountryName = "LTE_A" });
            countryLogic.Insert(new Country { CountryName = "LTE_B" });

            var all = countryLogic.Select(x => x.CountryName == "LTE_A"
                                             || x.CountryName == "LTE_B").ToList();
            int maxId = all.Max(c => c.CountryId);

            var result = countryLogic.Select(x => x.CountryId <= maxId
                                                && (x.CountryName == "LTE_A"
                                                 || x.CountryName == "LTE_B")).ToList();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Select_With_Contains()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "CTN", AirportName = "San Francisco International", CountryId = 501 });
            airportLogic.Insert(new Airport { AirportCode = "CT2", AirportName = "San Diego", CountryId = 501 });
            airportLogic.Insert(new Airport { AirportCode = "CT3", AirportName = "Chicago OHare", CountryId = 501 });

            var result = airportLogic.Select(x => x.CountryId == 501 && x.AirportName.Contains("San")).ToList();
            Assert.Equal(2, result.Count);
            Assert.All(result, a => Assert.Contains("San", a.AirportName));
        }

        [Fact]
        public void Select_With_StartsWith()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "SW1", AirportName = "New York JFK", CountryId = 502 });
            airportLogic.Insert(new Airport { AirportCode = "SW2", AirportName = "New Orleans", CountryId = 502 });
            airportLogic.Insert(new Airport { AirportCode = "SW3", AirportName = "Los Angeles", CountryId = 502 });

            var result = airportLogic.Select(x => x.CountryId == 502 && x.AirportName.StartsWith("New")).ToList();
            Assert.Equal(2, result.Count);
            Assert.All(result, a => Assert.StartsWith("New", a.AirportName));
        }

        [Fact]
        public void Select_With_EndsWith()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "EW1", AirportName = "Portland International", CountryId = 503 });
            airportLogic.Insert(new Airport { AirportCode = "EW2", AirportName = "San Francisco International", CountryId = 503 });
            airportLogic.Insert(new Airport { AirportCode = "EW3", AirportName = "Dallas Fort Worth", CountryId = 503 });

            var result = airportLogic.Select(x => x.CountryId == 503 && x.AirportName.EndsWith("International")).ToList();
            Assert.Equal(2, result.Count);
            Assert.All(result, a => Assert.EndsWith("International", a.AirportName));
        }

        [Fact]
        public void Count_With_NotEqual_Predicate()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "CN1", AirportName = "One", CountryId = 504 });
            airportLogic.Insert(new Airport { AirportCode = "CN2", AirportName = "Two", CountryId = 504 });
            airportLogic.Insert(new Airport { AirportCode = "CN3", AirportName = "Three", CountryId = 504 });

            Assert.Equal(2, airportLogic.Count(x => x.CountryId == 504 && x.AirportCode != "CN1"));
        }

        [Fact]
        public void Exists_With_GreaterThan_Predicate()
        {
            IReform<Country> countryLogic = _reformer.For<Country>();

            countryLogic.Insert(new Country { CountryName = "EGT_A" });
            var country = countryLogic.SelectSingle(x => x.CountryName == "EGT_A");

            Assert.True(countryLogic.Exists(x => x.CountryId >= country.CountryId && x.CountryName == "EGT_A"));
            Assert.False(countryLogic.Exists(x => x.CountryId > country.CountryId && x.CountryName == "EGT_A"));
        }

        [Fact]
        public void Select_With_Contains_Variable()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "CV1", AirportName = "Tokyo Narita", CountryId = 505 });
            airportLogic.Insert(new Airport { AirportCode = "CV2", AirportName = "Tokyo Haneda", CountryId = 505 });
            airportLogic.Insert(new Airport { AirportCode = "CV3", AirportName = "Osaka Kansai", CountryId = 505 });

            string searchTerm = "Tokyo";
            var result = airportLogic.Select(x => x.CountryId == 505 && x.AirportName.Contains(searchTerm)).ToList();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Select_With_Combined_Comparison_Operators()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "CC1", AirportName = "Test1", CountryId = 506 });
            airportLogic.Insert(new Airport { AirportCode = "CC2", AirportName = "Test2", CountryId = 507 });
            airportLogic.Insert(new Airport { AirportCode = "CC3", AirportName = "Test3", CountryId = 508 });

            var result = airportLogic.Select(x => x.CountryId >= 506 && x.CountryId <= 507).ToList();
            Assert.Equal(2, result.Count);
            Assert.Contains(result, a => a.AirportCode == "CC1");
            Assert.Contains(result, a => a.AirportCode == "CC2");
        }

        #endregion

        #region Sorting and Pagination Tests

        [Fact]
        public void Select_With_Sort_Ascending()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "SA3", AirportName = "Charlie", CountryId = 600 });
            airportLogic.Insert(new Airport { AirportCode = "SA1", AirportName = "Alpha", CountryId = 600 });
            airportLogic.Insert(new Airport { AirportCode = "SA2", AirportName = "Bravo", CountryId = 600 });

            var criteria = new QueryCriteria<Airport>
            {
                Predicate = x => x.CountryId == 600,
                SortCriteria = new SortCriteria
                {
                    SortCriterion.Ascending("AirportName")
                }
            };

            var result = airportLogic.Select(criteria).ToList();
            Assert.Equal(3, result.Count);
            Assert.Equal("Alpha", result[0].AirportName);
            Assert.Equal("Bravo", result[1].AirportName);
            Assert.Equal("Charlie", result[2].AirportName);
        }

        [Fact]
        public void Select_With_Sort_Descending()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "SD1", AirportName = "Alpha", CountryId = 601 });
            airportLogic.Insert(new Airport { AirportCode = "SD2", AirportName = "Bravo", CountryId = 601 });
            airportLogic.Insert(new Airport { AirportCode = "SD3", AirportName = "Charlie", CountryId = 601 });

            var criteria = new QueryCriteria<Airport>
            {
                Predicate = x => x.CountryId == 601,
                SortCriteria = new SortCriteria
                {
                    new SortCriterion("AirportName", SortDirection.Descending)
                }
            };

            var result = airportLogic.Select(criteria).ToList();
            Assert.Equal(3, result.Count);
            Assert.Equal("Charlie", result[0].AirportName);
            Assert.Equal("Bravo", result[1].AirportName);
            Assert.Equal("Alpha", result[2].AirportName);
        }

        [Fact]
        public void Select_With_Multiple_Sort_Criteria()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "MS1", AirportName = "Alpha", CountryId = 602 });
            airportLogic.Insert(new Airport { AirportCode = "MS3", AirportName = "Bravo", CountryId = 603 });
            airportLogic.Insert(new Airport { AirportCode = "MS2", AirportName = "Alpha", CountryId = 603 });

            var criteria = new QueryCriteria<Airport>
            {
                Predicate = x => x.CountryId == 602 || x.CountryId == 603,
                SortCriteria = new SortCriteria
                {
                    SortCriterion.Ascending("AirportName"),
                    new SortCriterion("AirportCode", SortDirection.Descending)
                }
            };

            var result = airportLogic.Select(criteria).ToList();
            Assert.Equal(3, result.Count);
            Assert.Equal("MS2", result[0].AirportCode); // Alpha, MS2 (desc by code)
            Assert.Equal("MS1", result[1].AirportCode); // Alpha, MS1
            Assert.Equal("MS3", result[2].AirportCode); // Bravo
        }

        [Fact]
        public void Select_With_Sort_No_Predicate()
        {
            IReform<Country> countryLogic = _reformer.For<Country>();

            countryLogic.Insert(new Country { CountryName = "Zambia" });
            countryLogic.Insert(new Country { CountryName = "Albania" });
            countryLogic.Insert(new Country { CountryName = "Morocco" });

            var criteria = new QueryCriteria<Country>
            {
                SortCriteria = new SortCriteria
                {
                    SortCriterion.Ascending("CountryName")
                }
            };

            var result = countryLogic.Select(criteria).ToList();
            Assert.True(result.Count >= 3);
            // Verify ordering: each name should be <= the next
            for (int i = 0; i < result.Count - 1; i++)
            {
                Assert.True(string.Compare(result[i].CountryName, result[i + 1].CountryName, StringComparison.Ordinal) <= 0);
            }
        }

        [Fact]
        public void Select_With_Pagination()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "PG1", AirportName = "Page Alpha", CountryId = 700 });
            airportLogic.Insert(new Airport { AirportCode = "PG2", AirportName = "Page Bravo", CountryId = 700 });
            airportLogic.Insert(new Airport { AirportCode = "PG3", AirportName = "Page Charlie", CountryId = 700 });
            airportLogic.Insert(new Airport { AirportCode = "PG4", AirportName = "Page Delta", CountryId = 700 });
            airportLogic.Insert(new Airport { AirportCode = "PG5", AirportName = "Page Echo", CountryId = 700 });

            var page1 = new QueryCriteria<Airport>
            {
                Predicate = x => x.CountryId == 700,
                SortCriteria = new SortCriteria { SortCriterion.Ascending("AirportName") },
                PageCriteria = new PageCriteria(1, 2)
            };

            var result1 = airportLogic.Select(page1).ToList();
            Assert.Equal(2, result1.Count);
            Assert.Equal("Page Alpha", result1[0].AirportName);
            Assert.Equal("Page Bravo", result1[1].AirportName);

            var page2 = new QueryCriteria<Airport>
            {
                Predicate = x => x.CountryId == 700,
                SortCriteria = new SortCriteria { SortCriterion.Ascending("AirportName") },
                PageCriteria = new PageCriteria(2, 2)
            };

            var result2 = airportLogic.Select(page2).ToList();
            Assert.Equal(2, result2.Count);
            Assert.Equal("Page Charlie", result2[0].AirportName);
            Assert.Equal("Page Delta", result2[1].AirportName);

            var page3 = new QueryCriteria<Airport>
            {
                Predicate = x => x.CountryId == 700,
                SortCriteria = new SortCriteria { SortCriterion.Ascending("AirportName") },
                PageCriteria = new PageCriteria(3, 2)
            };

            var result3 = airportLogic.Select(page3).ToList();
            Assert.Single(result3);
            Assert.Equal("Page Echo", result3[0].AirportName);
        }

        [Fact]
        public void Select_Paged_Beyond_Data_Returns_Empty()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "PE1", AirportName = "Only One", CountryId = 701 });

            var criteria = new QueryCriteria<Airport>
            {
                Predicate = x => x.CountryId == 701,
                SortCriteria = new SortCriteria { SortCriterion.Ascending("AirportName") },
                PageCriteria = new PageCriteria(2, 10)
            };

            var result = airportLogic.Select(criteria).ToList();
            Assert.Empty(result);
        }

        [Fact]
        public void Select_Paged_Without_Sort_Uses_PrimaryKey()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "NS1", AirportName = "NoSort Alpha", CountryId = 710 });
            airportLogic.Insert(new Airport { AirportCode = "NS2", AirportName = "NoSort Bravo", CountryId = 710 });
            airportLogic.Insert(new Airport { AirportCode = "NS3", AirportName = "NoSort Charlie", CountryId = 710 });

            var criteria = new QueryCriteria<Airport>
            {
                Predicate = x => x.CountryId == 710,
                PageCriteria = new PageCriteria(1, 2)
            };

            // Should succeed — CommandBuilder adds PK sort automatically
            var result = airportLogic.Select(criteria).ToList();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Select_With_Pagination_Descending()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "PD1", AirportName = "Alpha", CountryId = 702 });
            airportLogic.Insert(new Airport { AirportCode = "PD2", AirportName = "Bravo", CountryId = 702 });
            airportLogic.Insert(new Airport { AirportCode = "PD3", AirportName = "Charlie", CountryId = 702 });
            airportLogic.Insert(new Airport { AirportCode = "PD4", AirportName = "Delta", CountryId = 702 });

            var criteria = new QueryCriteria<Airport>
            {
                Predicate = x => x.CountryId == 702,
                SortCriteria = new SortCriteria
                {
                    new SortCriterion("AirportName", SortDirection.Descending)
                },
                PageCriteria = new PageCriteria(1, 2)
            };

            var result = airportLogic.Select(criteria).ToList();
            Assert.Equal(2, result.Count);
            Assert.Equal("Delta", result[0].AirportName);
            Assert.Equal("Charlie", result[1].AirportName);
        }

        [Fact]
        public void Select_Sort_By_Invalid_Property_Throws()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            var criteria = new QueryCriteria<Airport>
            {
                SortCriteria = new SortCriteria
                {
                    SortCriterion.Ascending("NonExistentProperty")
                }
            };

            Assert.Throws<InvalidOperationException>(() => airportLogic.Select(criteria).ToList());
        }

        [Fact]
        public async Task Select_With_Sort_Async()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            await airportLogic.InsertAsync(new Airport { AirportCode = "AS3", AirportName = "Charlie", CountryId = 800 });
            await airportLogic.InsertAsync(new Airport { AirportCode = "AS1", AirportName = "Alpha", CountryId = 800 });
            await airportLogic.InsertAsync(new Airport { AirportCode = "AS2", AirportName = "Bravo", CountryId = 800 });

            var criteria = new QueryCriteria<Airport>
            {
                Predicate = x => x.CountryId == 800,
                SortCriteria = new SortCriteria
                {
                    SortCriterion.Ascending("AirportCode")
                }
            };

            var result = (await airportLogic.SelectAsync(criteria)).ToList();
            Assert.Equal(3, result.Count);
            Assert.Equal("AS1", result[0].AirportCode);
            Assert.Equal("AS2", result[1].AirportCode);
            Assert.Equal("AS3", result[2].AirportCode);
        }

        [Fact]
        public async Task Select_With_Pagination_Async()
        {
            IReform<Airport> airportLogic = _reformer.For<Airport>();

            await airportLogic.InsertAsync(new Airport { AirportCode = "AP1", AirportName = "Alpha", CountryId = 801 });
            await airportLogic.InsertAsync(new Airport { AirportCode = "AP2", AirportName = "Bravo", CountryId = 801 });
            await airportLogic.InsertAsync(new Airport { AirportCode = "AP3", AirportName = "Charlie", CountryId = 801 });

            var criteria = new QueryCriteria<Airport>
            {
                Predicate = x => x.CountryId == 801,
                SortCriteria = new SortCriteria { SortCriterion.Ascending("AirportName") },
                PageCriteria = new PageCriteria(1, 2)
            };

            var result = (await airportLogic.SelectAsync(criteria)).ToList();
            Assert.Equal(2, result.Count);
            Assert.Equal("Alpha", result[0].AirportName);
            Assert.Equal("Bravo", result[1].AirportName);
        }

        #endregion

        #region Transaction Tests

        [Fact]
        public void Transaction_Insert_Update_Delete()
        {
            var countryLogic = _reformer.For<Country>();

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
            var countryLogic = _reformer.For<Country>();

            var countBefore = countryLogic.Count();

            using (var connection = countryLogic.GetConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    countryLogic.Insert(connection, transaction, new Country { CountryName = "Japan" });
                    countryLogic.Insert(connection, transaction, new Country { CountryName = "China" });
                    transaction.Rollback();
                }
            }

            Assert.Equal(countBefore, countryLogic.Count());
        }

        [Fact]
        public async Task Transaction_Async_Insert_Update_Delete()
        {
            var countryLogic = _reformer.For<Country>();

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
