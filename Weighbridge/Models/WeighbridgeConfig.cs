using System.IO.Ports;

namespace Weighbridge.Models
{
    public class WeighbridgeConfig
    {
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        public string? RegexString { get; set; }
        public bool StabilityEnabled { get; set; } = true;
        public double StableTime { get; set; } = 3.0;
        public string? StabilityRegex { get; set; }
        public bool UseZeroStringDetection { get; set; } = false;
        public string? ZeroString { get; set; } = "ZERO";
        public double ZeroTolerance { get; set; } = 0.1; // Default value
        public double RegulatoryZeroTolerance { get; set; } = 0.01; // New regulatory zero tolerance
        public bool RequireManualZeroConfirmation { get; set; } = false;
        public bool BypassZeroRequirement { get; set; } = true;
    }
}