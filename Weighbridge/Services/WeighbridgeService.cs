using System.IO.Ports;
using System.Text.RegularExpressions;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public class WeighbridgeService
    {
        private SerialPort _serialPort;
        private WeighbridgeConfig _config;
        private readonly WeightParserService _parserService;

        public event EventHandler<WeightReading>? DataReceived;
        public event EventHandler<string>? RawDataReceived;

        public WeighbridgeService(WeightParserService parserService)
        {
            _parserService = parserService;
            _config = new WeighbridgeConfig();

            // Load saved settings safely
            _config.PortName = Preferences.Get("PortName", "COM1");
            if (int.TryParse(Preferences.Get("BaudRate", "9600"), out int baudRate))
            {
                _config.BaudRate = baudRate;
            }
            else
            {
                _config.BaudRate = 9600; // Default value
            }
            _config.RegexString = Preferences.Get("RegexString", @"(?<sign>[+-])?(?<num>\d+(?:\.\d+)?)[ ]*(?<unit>[a-zA-Z]{1,4})");

            _serialPort = new SerialPort(
                _config.PortName,
                _config.BaudRate,
                _config.Parity,
                _config.DataBits,
                _config.StopBits
            );
        }

        public void Configure(WeighbridgeConfig config)
        {
            bool wasOpen = _serialPort.IsOpen;
            if (wasOpen)
            {
                Close();
            }

            _config = config;
            _serialPort.PortName = _config.PortName;
            _serialPort.BaudRate = _config.BaudRate;
            _serialPort.Parity = _config.Parity;
            _serialPort.DataBits = _config.DataBits;
            _serialPort.StopBits = _config.StopBits;

            if (wasOpen)
            {
                Open();
            }
        }
        public WeighbridgeConfig GetConfig()
        {
            return _config;
        }

        public void Open()
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.DataReceived += OnDataReceived;
                _serialPort.Open();
            }
        }

        public void Close()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.DataReceived -= OnDataReceived;
                _serialPort.Close();
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();

            // Fire the raw data event for the settings page preview
            RawDataReceived?.Invoke(this, indata);

            var weightReading = _parserService.Parse(indata, _config.RegexString);
            if (weightReading != null)
            {
                // Fire the parsed data event for the main page
                DataReceived?.Invoke(this, weightReading);
            }
        }

        public string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }
    }
}