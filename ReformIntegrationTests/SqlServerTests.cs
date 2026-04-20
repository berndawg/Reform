using Reform;
using Reform.Interfaces;
using ReformIntegrationTests.Objects;

namespace ReformIntegrationTests
{
    internal class SqlServerTests(ReformFactory reformFactory, string connectionString)
    {
        public void RunAll(TestRunner runner)
        {
            runner.Run("Insert_And_Count", Insert_And_Count);
            runner.Run("Count_With_Predicate", Count_With_Predicate);
            runner.Run("Exists_Returns_True_When_Found", Exists_Returns_True_When_Found);
            runner.Run("Exists_Returns_False_When_Not_Found", Exists_Returns_False_When_Not_Found);
            runner.Run("Select_All", Select_All);
            runner.Run("Select_With_Lambda", Select_With_Lambda);
            runner.Run("SelectSingle", SelectSingle);
            runner.Run("SelectSingleOrDefault_Returns_Null", SelectSingleOrDefault_Returns_Null);
            runner.Run("Update_Modifies_Record", Update_Modifies_Record);
            runner.Run("Update_No_Changes_Is_Noop", Update_No_Changes_Is_Noop);
            runner.Run("Delete_Removes_Record", Delete_Removes_Record);
            runner.Run("Delete_List_Removes_Multiple", Delete_List_Removes_Multiple);
            runner.Run("Insert_List_Inserts_Multiple", Insert_List_Inserts_Multiple);
            runner.Run("Select_With_And_Predicate", Select_With_And_Predicate);
            runner.Run("Select_With_Or_Predicate", Select_With_Or_Predicate);
            runner.Run("Validation_Throws_On_Missing_Required_Field", Validation_Throws_On_Missing_Required_Field);
            runner.Run("Validation_Throws_On_Default_Required_Int", Validation_Throws_On_Default_Required_Int);
            runner.Run("Merge_Inserts_New_Items", Merge_Inserts_New_Items);
            runner.Run("Merge_Updates_Existing_Items", Merge_Updates_Existing_Items);
            runner.Run("Merge_Deletes_Missing_Items", Merge_Deletes_Missing_Items);
            runner.Run("Merge_Full_Reconciliation", Merge_Full_Reconciliation);
            runner.Run("Merge_Empty_List_Throws", Merge_Empty_List_Throws);
            runner.Run("Merge_With_No_Changes", Merge_With_No_Changes);
            runner.Run("Truncate_Removes_All_Rows", Truncate_Removes_All_Rows);
            runner.Run("Transaction_Insert_Update_Delete", Transaction_Insert_Update_Delete);
            runner.Run("Transaction_Rollback", Transaction_Rollback);
        }

        private void Clean() => DatabaseSetup.CleanTables(connectionString);

        #region CRUD

        private void Insert_And_Count()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

            countryLogic.Insert(new Country { CountryName = "Morocco" });
            countryLogic.Insert(new Country { CountryName = "United States" });
            countryLogic.Insert(new Country { CountryName = "Iceland" });

            Assert.Equal(3, countryLogic.Count());
        }

        private void Count_With_Predicate()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();
            var airportLogic = reformFactory.For<Airport>();

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

        private void Exists_Returns_True_When_Found()
        {
            Clean();
            var airportLogic = reformFactory.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "RAK", AirportName = "Marrakesh", CountryId = 1 });

            Assert.True(airportLogic.Exists(x => x.AirportCode == "RAK"));
        }

        private void Exists_Returns_False_When_Not_Found()
        {
            Clean();
            var airportLogic = reformFactory.For<Airport>();

            Assert.False(airportLogic.Exists(x => x.AirportCode == "NONEXISTENT"));
        }

        private void Select_All()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

            countryLogic.Insert(new Country { CountryName = "France" });
            countryLogic.Insert(new Country { CountryName = "Germany" });

            var all = countryLogic.Select().ToList();
            Assert.True(all.Count >= 2);
        }

        private void Select_With_Lambda()
        {
            Clean();
            var airportLogic = reformFactory.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "CDG", AirportName = "Charles de Gaulle", CountryId = 10 });
            airportLogic.Insert(new Airport { AirportCode = "ORY", AirportName = "Orly", CountryId = 10 });
            airportLogic.Insert(new Airport { AirportCode = "FCO", AirportName = "Fiumicino", CountryId = 20 });

            var frenchAirports = airportLogic.Select(x => x.CountryId == 10).ToList();
            Assert.Equal(2, frenchAirports.Count);
        }

        private void SelectSingle()
        {
            Clean();
            var airportLogic = reformFactory.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "NRT", AirportName = "Narita", CountryId = 30 });

            var airport = airportLogic.SelectSingle(x => x.AirportCode == "NRT");
            Assert.Equal("Narita", airport.AirportName);
        }

        private void SelectSingleOrDefault_Returns_Null()
        {
            Clean();
            var airportLogic = reformFactory.For<Airport>();

            var airport = airportLogic.SelectSingleOrDefault(x => x.AirportCode == "ZZZZZ");
            Assert.Null(airport);
        }

        private void Update_Modifies_Record()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

            var country = new Country { CountryName = "Moroco" };
            countryLogic.Insert(country);

            country.CountryName = "Morocco";
            countryLogic.Update(country);

            var updated = countryLogic.SelectSingle(x => x.CountryId == country.CountryId);
            Assert.Equal("Morocco", updated.CountryName);
        }

        private void Update_No_Changes_Is_Noop()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

            var country = new Country { CountryName = "France" };
            countryLogic.Insert(country);

            countryLogic.Update(country);

            var result = countryLogic.SelectSingle(x => x.CountryId == country.CountryId);
            Assert.Equal("France", result.CountryName);
        }

        private void Delete_Removes_Record()
        {
            Clean();
            var airportLogic = reformFactory.For<Airport>();

            var airport = new Airport { AirportCode = "DEL", AirportName = "Delhi", CountryId = 40 };
            airportLogic.Insert(airport);

            Assert.True(airportLogic.Exists(x => x.AirportCode == "DEL"));

            airportLogic.Delete(airport);

            Assert.False(airportLogic.Exists(x => x.AirportCode == "DEL"));
        }

        private void Delete_List_Removes_Multiple()
        {
            Clean();
            var airportLogic = reformFactory.For<Airport>();

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

        private void Insert_List_Inserts_Multiple()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

            var before = countryLogic.Count();

            countryLogic.Insert(new List<Country>
            {
                new Country { CountryName = "Japan" },
                new Country { CountryName = "Brazil" }
            });

            var after = countryLogic.Count();
            Assert.Equal(before + 2, after);
        }

        private void Select_With_And_Predicate()
        {
            Clean();
            var airportLogic = reformFactory.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "JFK", AirportName = "John F Kennedy", CountryId = 100 });
            airportLogic.Insert(new Airport { AirportCode = "EWR", AirportName = "Newark", CountryId = 100 });

            var result = airportLogic.Select(x => x.CountryId == 100 && x.AirportCode == "JFK").ToList();
            Assert.Single(result);
            Assert.Equal("John F Kennedy", result[0].AirportName);
        }

        private void Select_With_Or_Predicate()
        {
            Clean();
            var airportLogic = reformFactory.For<Airport>();

            airportLogic.Insert(new Airport { AirportCode = "SYD", AirportName = "Sydney", CountryId = 200 });
            airportLogic.Insert(new Airport { AirportCode = "MEL", AirportName = "Melbourne", CountryId = 200 });
            airportLogic.Insert(new Airport { AirportCode = "BNE", AirportName = "Brisbane", CountryId = 200 });

            var result = airportLogic.Select(x => x.AirportCode == "SYD" || x.AirportCode == "MEL").ToList();
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region Validation

        private void Validation_Throws_On_Missing_Required_Field()
        {
            Clean();
            var airportLogic = reformFactory.For<Airport>();

            var airport = new Airport { AirportCode = "", AirportName = "Test", CountryId = 1 };

            Assert.Throws<ArgumentException>(() => airportLogic.Insert(airport));
        }

        private void Validation_Throws_On_Default_Required_Int()
        {
            Clean();
            var airportLogic = reformFactory.For<Airport>();

            var airport = new Airport { AirportCode = "TST", AirportName = "Test", CountryId = 0 };

            Assert.Throws<ArgumentException>(() => airportLogic.Insert(airport));
        }

        #endregion

        #region Merge

        private void Merge_Inserts_New_Items()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

            countryLogic.Merge(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" }
            });

            Assert.Equal(2, countryLogic.Count());
            Assert.True(countryLogic.Exists(x => x.CountryName == "France"));
            Assert.True(countryLogic.Exists(x => x.CountryName == "Germany"));
        }

        private void Merge_Updates_Existing_Items()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

            countryLogic.Insert(new Country { CountryName = "France" });
            var france = countryLogic.SelectSingle(x => x.CountryName == "France");

            france.CountryName = "French Republic";
            countryLogic.Merge(new List<Country> { france });

            Assert.Equal(1, countryLogic.Count());
            var updated = countryLogic.SelectSingle(x => x.CountryId == france.CountryId);
            Assert.Equal("French Republic", updated.CountryName);
        }

        private void Merge_Deletes_Missing_Items()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

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

        private void Merge_Full_Reconciliation()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

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
                france,
                germany,
                new Country { CountryName = "Italy" }
            });

            Assert.Equal(3, countryLogic.Count());
            Assert.True(countryLogic.Exists(x => x.CountryName == "French Republic"));
            Assert.True(countryLogic.Exists(x => x.CountryName == "Germany"));
            Assert.True(countryLogic.Exists(x => x.CountryName == "Italy"));
            Assert.False(countryLogic.Exists(x => x.CountryName == "Spain"));
            Assert.False(countryLogic.Exists(x => x.CountryName == "France"));
        }

        private void Merge_Empty_List_Throws()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

            countryLogic.Insert(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" }
            });

            Assert.Throws<ArgumentException>(() => countryLogic.Merge(new List<Country>()));

            Assert.Equal(2, countryLogic.Count());
        }

        private void Merge_With_No_Changes()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

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

        #endregion

        #region Truncate

        private void Truncate_Removes_All_Rows()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

            countryLogic.Insert(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" }
            });

            Assert.Equal(2, countryLogic.Count());

            countryLogic.Truncate();

            Assert.Equal(0, countryLogic.Count());
        }

        #endregion

        #region Transactions

        private void Transaction_Insert_Update_Delete()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

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

        private void Transaction_Rollback()
        {
            Clean();
            var countryLogic = reformFactory.For<Country>();

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

        #endregion
    }
}
