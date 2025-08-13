using System.Text.RegularExpressions;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public class WeightParserService
    {
        public WeightReading Parse(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            // Regex to find a decimal number
            var numberMatch = Regex.Match(data, @"\d+(\.\d+)?");

            if (!numberMatch.Success)
            {
                return null;
            }

            var weight = decimal.Parse(numberMatch.Value);
            var unit = "KG"; // Default unit

            // Check for units
            if (data.ToUpper().Contains("LBS"))
            {
                unit = "LBS";
            }
            else if (data.ToUpper().Contains("KG"))
            {
                unit = "KG";
            }

            return new WeightReading { Weight = weight, Unit = unit };
        }
    }
}
