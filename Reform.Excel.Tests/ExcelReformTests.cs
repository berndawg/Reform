using System.Data;
using Reform.Excel.Tests.Objects;
using Reform.Interfaces;
using Reform.Logic;

namespace Reform.Excel.Tests
{
    public class ExcelReformTests : IDisposable
    {
        private readonly string _filePath;
        private readonly IReform<Country> _countryRepo;
        private readonly IReform<Airport> _airportRepo;

        public ExcelReformTests()
        {
            _filePath = Path.Combine(Path.GetTempPath(), $"ReformExcelTest_{Guid.NewGuid()}.xlsx");

            var countryMetadata = new MetadataProvider<Country>();
            var airportMetadata = new MetadataProvider<Airport>();

            _countryRepo = new ExcelReform<Country>(_filePath, countryMetadata, new Validator<Country>(countryMetadata));
            _airportRepo = new ExcelReform<Airport>(_filePath, airportMetadata, new Validator<Airport>(airportMetadata));
        }

        public void Dispose()
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }

        #region Ported from legacy ExcelTests

        [Fact]
        public void Insert_And_Count()
        {
            _countryRepo.Insert(new Country { CountryName = "Morocco" });
            _countryRepo.Insert(new Country { CountryName = "Iceland" });
            _countryRepo.Insert(new Country { CountryName = "Japan" });

            Assert.Equal(3, _countryRepo.Count());
        }

        [Fact]
        public void Insert_Auto_Increments_Identity()
        {
            _countryRepo.Insert(new Country { CountryName = "France" });
            _countryRepo.Insert(new Country { CountryName = "Germany" });

            var countries = _countryRepo.Select().ToList();

            Assert.Equal(1, countries[0].CountryId);
            Assert.Equal(2, countries[1].CountryId);
        }

        [Fact]
        public void Select_All()
        {
            _countryRepo.Insert(new Country { CountryName = "Peru" });
            _countryRepo.Insert(new Country { CountryName = "Chile" });

            var all = _countryRepo.Select().ToList();
            Assert.Equal(2, all.Count);
        }

        [Fact]
        public void Select_With_Predicate()
        {
            _airportRepo.Insert(new Airport { AirportCode = "CDG", AirportName = "Charles de Gaulle", CountryId = 1 });
            _airportRepo.Insert(new Airport { AirportCode = "ORY", AirportName = "Orly", CountryId = 1 });
            _airportRepo.Insert(new Airport { AirportCode = "FCO", AirportName = "Fiumicino", CountryId = 2 });

            var french = _airportRepo.Select(x => x.CountryId == 1).ToList();
            Assert.Equal(2, french.Count);
        }

        [Fact]
        public void SelectSingle()
        {
            _airportRepo.Insert(new Airport { AirportCode = "NRT", AirportName = "Narita", CountryId = 5 });

            var airport = _airportRepo.SelectSingle(x => x.AirportCode == "NRT");
            Assert.Equal("Narita", airport.AirportName);
        }

        [Fact]
        public void SelectSingleOrDefault_Returns_Null_When_Not_Found()
        {
            var airport = _airportRepo.SelectSingleOrDefault(x => x.AirportCode == "ZZZZZ");
            Assert.Null(airport);
        }

        [Fact]
        public void Count_With_Predicate()
        {
            _airportRepo.Insert(new Airport { AirportCode = "LAX", AirportName = "Los Angeles", CountryId = 10 });
            _airportRepo.Insert(new Airport { AirportCode = "SFO", AirportName = "San Francisco", CountryId = 10 });
            _airportRepo.Insert(new Airport { AirportCode = "LIM", AirportName = "Lima", CountryId = 20 });

            Assert.Equal(2, _airportRepo.Count(x => x.CountryId == 10));
            Assert.Equal(1, _airportRepo.Count(x => x.CountryId == 20));
        }

        [Fact]
        public void Exists_Returns_True_When_Found()
        {
            _airportRepo.Insert(new Airport { AirportCode = "RAK", AirportName = "Marrakesh", CountryId = 1 });

            Assert.True(_airportRepo.Exists(x => x.AirportCode == "RAK"));
        }

        [Fact]
        public void Exists_Returns_False_When_Not_Found()
        {
            Assert.False(_airportRepo.Exists(x => x.AirportCode == "NONEXISTENT"));
        }

        [Fact]
        public void Update_Modifies_Record()
        {
            var country = new Country { CountryName = "Moroco" };
            _countryRepo.Insert(country);

            country.CountryName = "Morocco";
            _countryRepo.Update(country);

            var updated = _countryRepo.SelectSingle(x => x.CountryId == country.CountryId);
            Assert.Equal("Morocco", updated.CountryName);
        }

        [Fact]
        public void Delete_Removes_Record()
        {
            var airport = new Airport { AirportCode = "DEL", AirportName = "Delhi", CountryId = 40 };
            _airportRepo.Insert(airport);

            Assert.True(_airportRepo.Exists(x => x.AirportCode == "DEL"));

            _airportRepo.Delete(airport);

            Assert.False(_airportRepo.Exists(x => x.AirportCode == "DEL"));
        }

        [Fact]
        public void Insert_List_Inserts_Multiple()
        {
            _countryRepo.Insert(new List<Country>
            {
                new Country { CountryName = "Argentina" },
                new Country { CountryName = "Brazil" }
            });

            Assert.Equal(2, _countryRepo.Count());
        }

        [Fact]
        public void Delete_List_Removes_Multiple()
        {
            var a1 = new Airport { AirportCode = "XX1", AirportName = "Test1", CountryId = 50 };
            var a2 = new Airport { AirportCode = "XX2", AirportName = "Test2", CountryId = 50 };
            _airportRepo.Insert(a1);
            _airportRepo.Insert(a2);

            Assert.Equal(2, _airportRepo.Count());

            _airportRepo.Delete(new List<Airport> { a1, a2 });

            Assert.Equal(0, _airportRepo.Count());
        }

        [Fact]
        public void Validation_Throws_On_Missing_Required_Field()
        {
            var airport = new Airport { AirportCode = "", AirportName = "Test", CountryId = 1 };
            Assert.Throws<ArgumentException>(() => _airportRepo.Insert(airport));
        }

        [Fact]
        public void Multiple_Entity_Types_In_Same_Workbook()
        {
            _countryRepo.Insert(new Country { CountryName = "Japan" });
            _airportRepo.Insert(new Airport { AirportCode = "NRT", AirportName = "Narita", CountryId = 1 });

            Assert.Equal(1, _countryRepo.Count());
            Assert.Equal(1, _airportRepo.Count());

            var country = _countryRepo.SelectSingle(x => x.CountryName == "Japan");
            var airport = _airportRepo.SelectSingle(x => x.AirportCode == "NRT");

            Assert.Equal("Japan", country.CountryName);
            Assert.Equal("Narita", airport.AirportName);
        }

        #endregion

        #region New: Merge, Truncate, NotSupported, Async

        [Fact]
        public void Merge_Inserts_New_Items()
        {
            _countryRepo.Merge(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" }
            });

            Assert.Equal(2, _countryRepo.Count());
        }

        [Fact]
        public void Merge_Updates_And_Deletes_To_Reconcile()
        {
            _countryRepo.Insert(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" },
                new Country { CountryName = "Spain" }
            });

            var france = _countryRepo.SelectSingle(c => c.CountryName == "France");
            france.CountryName = "French Republic";

            _countryRepo.Merge(new List<Country>
            {
                france,
                new Country { CountryName = "Italy" }
            });

            Assert.Equal(2, _countryRepo.Count());
            Assert.True (_countryRepo.Exists(c => c.CountryName == "French Republic"));
            Assert.True (_countryRepo.Exists(c => c.CountryName == "Italy"));
            Assert.False(_countryRepo.Exists(c => c.CountryName == "Germany"));
            Assert.False(_countryRepo.Exists(c => c.CountryName == "Spain"));
        }

        [Fact]
        public void Merge_Empty_List_Throws()
        {
            Assert.Throws<ArgumentException>(() => _countryRepo.Merge(new List<Country>()));
        }

        [Fact]
        public void Truncate_Removes_All_Rows()
        {
            _countryRepo.Insert(new List<Country>
            {
                new Country { CountryName = "France" },
                new Country { CountryName = "Germany" }
            });
            Assert.Equal(2, _countryRepo.Count());

            _countryRepo.Truncate();

            Assert.Equal(0, _countryRepo.Count());
        }

        [Fact]
        public void GetConnection_Throws_NotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => _countryRepo.GetConnection());
        }

        [Fact]
        public void Insert_With_IDbConnection_Throws_NotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() =>
                _countryRepo.Insert(connection: null!, new Country { CountryName = "France" }));
            Assert.Throws<NotSupportedException>(() =>
                _countryRepo.Insert(connection: null!, transaction: null!, new Country { CountryName = "France" }));
        }

        [Fact]
        public async Task Async_Operations_Round_Trip()
        {
            await _countryRepo.InsertAsync(new Country { CountryName = "France" });
            await _countryRepo.InsertAsync(new Country { CountryName = "Germany" });

            Assert.Equal(2, await _countryRepo.CountAsync());
            Assert.True(await _countryRepo.ExistsAsync(c => c.CountryName == "France"));

            var france = await _countryRepo.SelectSingleAsync(c => c.CountryName == "France");
            france.CountryName = "French Republic";
            await _countryRepo.UpdateAsync(france);

            Assert.True(await _countryRepo.ExistsAsync(c => c.CountryName == "French Republic"));

            await _countryRepo.DeleteAsync(france);
            Assert.Equal(1, await _countryRepo.CountAsync());

            await _countryRepo.TruncateAsync();
            Assert.Equal(0, await _countryRepo.CountAsync());
        }

        #endregion
    }

    public class ReformerUseExcelTests
    {
        [Fact]
        public void UseExcel_Returns_ExcelReform_For_Registered_Type()
        {
            var filePath = Path.Combine(Path.GetTempPath(), $"ReformUseExcel_{Guid.NewGuid()}.xlsx");
            try
            {
                using var factory = new Reformer()
                    .UseSqlite("Data Source=:memory:")
                    .UseExcel<Country>(filePath)
                    .Build();

                var repo = factory.For<Country>();

                Assert.IsType<ExcelReform<Country>>(repo);
            }
            finally
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }

        [Fact]
        public void UseExcel_Single_Build_Can_Mix_Excel_And_Sql()
        {
            var filePath = Path.Combine(Path.GetTempPath(), $"ReformMix_{Guid.NewGuid()}.xlsx");
            try
            {
                using var factory = new Reformer()
                    .UseSqlite("Data Source=:memory:")
                    .UseExcel<Country>(filePath)
                    .Build();

                var countryRepo = factory.For<Country>();
                var airportRepo = factory.For<Airport>();

                Assert.IsType<ExcelReform<Country>>(countryRepo);
                Assert.IsNotType<ExcelReform<Airport>>(airportRepo);
            }
            finally
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }
    }
}
