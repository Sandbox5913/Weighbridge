using System.Text.RegularExpressions;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public class WeightParserService
    {
        public WeightReading Parse(string data, string regexPattern)
        {
            if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(regexPattern))
            {
                return null;
            }

            Match match = Regex.Match(data, regexPattern);

            if (!match.Success)
            {
                return null;
            }

            decimal weight = decimal.Parse(match.Groups["num"].Value);
            if (match.Groups["sign"].Value == "-")
            {
                weight *= -1;
            }

            string unit = match.Groups["unit"].Success ? match.Groups["unit"].Value.ToUpper() : "KG";

            return new WeightReading { Weight = weight, Unit = unit };
        }
    }
}