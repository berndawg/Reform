using Reform;
using Reform.Interfaces;
using ReformIntegrationTests.Objects;

namespace ReformIntegrationTests
{
    internal class SqlServerAsyncTests
    {
        private readonly ReformFactory _reformer;
        private readonly string _connectionString;

        public SqlServerAsyncTests(ReformFactory reformer, string connectionString)
        {
            _reformer = reformer;
            _connectionString = connectionString;
        }

        public async Task RunAll(TestRunner runner)
        {
            await runner.RunAsync("Insert_And_Count_Async", Insert_And_Count_Async);
            await runner.RunAsync("Select_Async", Select_Async);
            await runner.RunAsync("Update_Async", Update_Async);
            await runner.RunAsync("Delete_Async", Delete_Async);
            await runner.RunAsync("SelectSingleOrDefault_Async_Returns_Null", SelectSingleOrDefault_Async_Returns_Null);
            await runner.RunAsync("Exists_Async_Returns_False_When_Not_Found", Exists_Async_Returns_False_When_Not_Found);
            await runner.RunAsync("Merge_Async", Merge_Async);
            await runner.RunAsync("Truncate_Async", Truncate_Async);
            await runner.RunAsync("Transaction_Async_Insert_Update_Delete", Transaction_Async_Insert_Update_Delete);
        }

        private void Clean() => DatabaseSetup.CleanTables(_connectionString);

        private async Task Insert_And_Count_Async()
        {
            Clean();
            var countryLogic = _reformer.For<Country>();

            await countryLogic.InsertAsync(new Country { CountryName = "Argentina" });
            await countryLogic.InsertAsync(new Country { CountryName = "Chile" });

            Assert.Equal(2, await countryLogic.CountAsync());
        }

        private async Task Select_Async()
        {
            Clean();
            var airportLogic = _reformer.For<Airport>();

            await airportLogic.InsertAsync(new Airport { AirportCode = "GRU", AirportName = "Guarulhos", CountryId = 300 });
            await airportLogic.InsertAsync(new Airport { AirportCode = "GIG", AirportName = "Galeao", CountryId = 300 });

            var airports = (await airportLogic.SelectAsync(x => x.CountryId == 300)).ToList();
            Assert.Equal(2, airports.Count);
        }

        private async Task Update_Async()
        {
            Clean();
            var countryLogic = _reformer.For<Country>();

            var country = new Country { CountryName = "Barzil" };
            await countryLogic.InsertAsync(country);

            country.CountryName = "Brazil";
            await countryLogic.UpdateAsync(country);

            var updated = await countryLogic.SelectSingleAsync(x => x.CountryId == country.CountryId);
            Assert.Equal("Brazil", updated.CountryName);
        }

        private async Task Delete_Async()
        {
            Clean();
            var airportLogic = _reformer.For<Airport>();

            var airport = new Airport { AirportCode = "EZE", AirportName = "Ezeiza", CountryId = 400 };
            await airportLogic.InsertAsync(airport);

            Assert.True(await airportLogic.ExistsAsync(x => x.AirportCode == "EZE"));

            await airportLogic.DeleteAsync(airport);

            Assert.False(await airportLogic.ExistsAsync(x => x.AirportCode == "EZE"));
        }

        private async Task SelectSingleOrDefault_Async_Returns_Null()
        {
            Clean();
            var airportLogic = _reformer.For<Airport>();

            var airport = await airportLogic.SelectSingleOrDefaultAsync(x => x.AirportCode == "NOPE");
            Assert.Null(airport);
        }

        private async Task Exists_Async_Returns_False_When_Not_Found()
        {
            Clean();
            var airportLogic = _reformer.For<Airport>();

            Assert.False(await airportLogic.ExistsAsync(x => x.AirportCode == "NONEXISTENT"));
        }

        private async Task Merge_Async()
        {
            Clean();
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

        private async Task Truncate_Async()
        {
            Clean();
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

        private async Task Transaction_Async_Insert_Update_Delete()
        {
            Clean();
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
    }
}
