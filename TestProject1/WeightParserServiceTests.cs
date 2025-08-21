using NUnit.Framework;
using Weighbridge.Services;
using static NUnit.Framework.Assert;
namespace Weighbridge.Tests
{
    [TestFixture]
    public class WeightParserServiceTests
    {
        private WeightParserService _parser = null!;
        // This regex is an example. The actual regex would be configured in the app settings.
        private const string DefaultRegex = @"^\s*(?<sign>[-+])?(?<num>\d+(\.\d+)?)\s*(?<unit>kg|lb|t)?\s*$";

        [SetUp]
        public void Setup()
        {
            _parser = new WeightParserService();
        }
   

        [TestCase("2000 kg", 2000, "KG")]
        [TestCase("  1500.50 lb  ", 1500.50, "LB")]
        [TestCase("+100 t", 100, "T")]
        [TestCase("-50.25", -50.25, "KG")] // Assumes KG default
        [TestCase("12345", 12345, "KG")]
        [TestCase("0.123", 0.123, "KG")]
        public void Parse_WithValidData_ShouldReturnCorrectWeightReading(string data, double expectedWeight, string expectedUnit)
        {
            // Act
            var result = _parser.Parse(data, DefaultRegex);

            // Assert
            That(result, Is.Not.Null);
            That((decimal)expectedWeight, Is.EqualTo(result.Weight));
            That(expectedUnit, Is.EqualTo(result.Unit));
      
        }

        [TestCase(null, DefaultRegex)]
        [TestCase("", DefaultRegex)]
        [TestCase("  ", DefaultRegex)]
        [TestCase("some text", DefaultRegex)]
        [TestCase("1000 kgs", DefaultRegex)] // Does not match "kg"
        [TestCase("kg 1000", DefaultRegex)]
        [TestCase("10-00 kg", DefaultRegex)]
        public void Parse_WithInvalidOrEmptyData_ShouldReturnNull(string? data, string regex)
        {
            // Act
            var result = _parser.Parse(data, regex);

            // Assert
            That(result, Is.Null);

        }

        [Test]
        public void Parse_WithNullRegex_ShouldReturnNull()
        {
            // Act
            var result = _parser.Parse("1000 kg", null);

            // Assert
            That(result, Is.Null);
        }

        [Test]
        public void Parse_WithEmptyRegex_ShouldReturnNull()
        {
            // Act
            var result = _parser.Parse("1000 kg", "");

            // Assert
            That(result, Is.Null);

        }
        
        [Test]
        public void Parse_WithDifferentValidRegex_ShouldSucceed()
        {
            // Arrange
            var data = "Weight: 55.5 tonnes";
            var regex = @"Weight:\s*(?<num>\d+(\.\d+)?)\s*(?<unit>tonnes)";

            // Act
            var result = _parser.Parse(data, regex);


       

            // Assert
            That(result, Is.Not.Null);
            That(55.5m, Is.EqualTo(result.Weight));
            That("TONNES", Is.EqualTo(result.Unit));
        }
    }
}
