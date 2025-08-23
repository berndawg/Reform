using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reform.Interfaces;
using Reform.Logic;
using Reform.Objects;
using ReformTests.Logic;
using ReformTests.Objects;

namespace ReformTests
{
    [TestClass]
    public class UnitTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Reformer.RegisterType(typeof(IConnectionStringProvider), typeof(TestConnectionStringProvider));
            Reformer.RegisterType(typeof(IDebugLogger), typeof(TestDebugLogger));
        }

        [TestMethod]
        public void TestEverything()
        {
            IReform<Country> countryLogic = Reformer.Reform<Country>();
            IReform<Airport> airportLogic = Reformer.Reform<Airport>();

            // delete data from previous run
            airportLogic.Truncate();
            countryLogic.Truncate();

            // create a few countries
            var countries = new List<Country>
                {
                    new Country {CountryName = "Morocco"}, // 1
                    new Country {CountryName = "United States of America"}, // 2
                    new Country {CountryName = "Iceland"}, // 3
                    new Country {CountryName = "Peru"}, // 4
                    new Country {CountryName = "Mexico"}, // 5
                };

            countryLogic.BulkInsert(countries);

            // create a few airports
            var airports = new List<Airport>
                {
                    new Airport { AirportCode = "RAK", AirportName = "Marrakesh",   CountryId = 1 },
                    new Airport { AirportCode = "LIM", AirportName = "Lima",        CountryId = 4 },
                    new Airport { AirportCode = "RKV", AirportName = "Keplavik",    CountryId = 3 }
                };

            airportLogic.BulkInsert(airports);

            // There should be 3 airports
            Assert.AreEqual(3, airportLogic.Count());

            // LIM should be one of the airports
            Assert.IsTrue(airportLogic.Exists(Filter.EqualTo(nameof(Airport.AirportCode), "LIM")));

            // LAX should not be one of them
            Assert.IsFalse(airportLogic.Exists(Filter.EqualTo(nameof(Airport.AirportCode), "LAX")));

            // add (merge) 2 more American airports 
            airports.Add(new Airport { AirportCode = "AUS", AirportName = "Austin", CountryId = 2 });
            airports.Add(new Airport { AirportCode = "LAX", AirportName = "Los Angeles", CountryId = 2 });

            airportLogic.Merge(airports);

            // There should be 5 airports now
            Assert.AreEqual(5, airportLogic.Count());

            // LAX and AUS should both exist now
            Assert.IsTrue(airportLogic.Exists(Filter.EqualTo(nameof(Airport.AirportCode), "LAX")));
            Assert.IsTrue(airportLogic.Exists(Filter.EqualTo(nameof(Airport.AirportCode), "AUS")));

            var usaOnlyFilter = Filter.EqualTo(nameof(Airport.CountryId), 2);

            // Get all airports in USA, should be exactly 2 (LAX and AUS)
            List<Airport> usAirports = airportLogic.Select(usaOnlyFilter).ToList();
            Assert.AreEqual(2, usAirports.Count);

            // delete one of the USA airports (doesn't matter which)
            usAirports.Remove(usAirports[0]);

            // The merge should only delete 1 of the US airports because we gave it a "USA only" filter
            airportLogic.Merge(usAirports, usaOnlyFilter);

            // There should be 4 airports
            Assert.AreEqual(4, airportLogic.Count());

            var finalList = airportLogic.Select();

            foreach (Airport airport in finalList)
            {
                Console.WriteLine($"{airport.AirportCode} is in {airport.AirportName}");
            }
        }

        [TestMethod]
        public void GenerateCode()
        {
            ICodeGenerator codeGenerator = new CodeGenerator();

            Console.WriteLine(codeGenerator.GenerateCode("Airport"));
        }
    }

    internal class TestConnectionStringProvider : IConnectionStringProvider
    {
        public string GetConnectionString(string databaseName)
        {
            return $"Data Source=.;Initial Catalog={databaseName};Integrated Security=True";
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