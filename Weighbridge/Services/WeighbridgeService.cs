using System.IO.Ports;
using System.Text;
using Weighbridge.Models;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

namespace Weighbridge.Services
{
    public class WeighbridgeService : IWeighbridgeService
    {
        private SerialPort _serialPort;
        private WeighbridgeConfig _config;
        private readonly WeightParserService _parserService;
        private StringBuilder _serialDataBuffer = new StringBuilder();

        // --- Stability Detection ---
        private List<double> recentWeights = new List<double>();
        private const int windowSize = 10;
        private const double tolerance = 0.5;
        private DateTime stabilityStart;
        private bool stableFlag = false;

        public event EventHandler<WeightReading>? DataReceived;
        public event EventHandler<string>? RawDataReceived;
        public event EventHandler<bool>? StabilityChanged;

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
            _config.StabilityEnabled = Preferences.Get("StabilityEnabled", true);
            if (double.TryParse(Preferences.Get("StableTime", "3.0"), out double stableTime))
            {
                _config.StableTime = stableTime;
            }
            else
            {
                _config.StableTime = 3.0; // Default value
            }
            _config.StabilityRegex = Preferences.Get("StabilityRegex", "");

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
            _serialDataBuffer.Append(indata);

            string bufferString = _serialDataBuffer.ToString();
            // Look for any newline character (\n) or carriage return (\r)
            int newlineIndex;
            while ((newlineIndex = bufferString.IndexOfAny(new char[] { '\r', '\n' })) >= 0)
            {
                string line = bufferString.Substring(0, newlineIndex).Trim();
                bufferString = bufferString.Substring(newlineIndex + 1);
                // If the next character is also a newline character, remove it as well (to handle \r\n)
                if (bufferString.Length > 0 && (bufferString[0] == '\r' || bufferString[0] == '\n'))
                {
                    bufferString = bufferString.Substring(1);
                }


                if (!string.IsNullOrWhiteSpace(line))
                {
                    // Fire the raw data event for the settings page preview
                    RawDataReceived?.Invoke(this, line);

                    var weightReading = _parserService.Parse(line, _config.RegexString);
                    if (weightReading != null)
                    {
                        // Fire the parsed data event for the main page
                        DataReceived?.Invoke(this, weightReading);

                        // Check for stability
                        bool isStable = false;
                        if (_config.StabilityEnabled)
                        {
                            if (!string.IsNullOrEmpty(_config.StabilityRegex))
                            {
                                isStable = Regex.IsMatch(line, _config.StabilityRegex);
                            }
                            else
                            {
                                isStable = IsWeightStable((double)weightReading.Weight);
                            }
                        }
                        StabilityChanged?.Invoke(this, isStable);
                    }
                }
            }
            _serialDataBuffer = new StringBuilder(bufferString);
        }

        private bool IsWeightStable(double newWeight)
        {
            recentWeights.Add(newWeight);
            if (recentWeights.Count > windowSize)
                recentWeights.RemoveAt(0);

            if (recentWeights.Count < windowSize)
                return false; // not enough data yet

            double max = double.MinValue, min = double.MaxValue;
            foreach (var w in recentWeights)
            {
                if (w > max) max = w;
                if (w < min) min = w;
            }

            bool inTolerance = (max - min) <= tolerance;

            if (inTolerance)
            {
                if (stabilityStart == DateTime.MinValue)
                    stabilityStart = DateTime.Now;

                if ((DateTime.Now - stabilityStart).TotalSeconds >= _config.StableTime)
                    return true;
            }
            else
            {
                stabilityStart = DateTime.MinValue; // reset timer if out of tolerance
            }

            return false;
        }


        public string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }
    }
}