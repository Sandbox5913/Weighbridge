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
        private Timer _simulationTimer;

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

            try
            {
                _serialPort = new SerialPort(
                    _config.PortName,
                    _config.BaudRate,
                    _config.Parity,
                    _config.DataBits,
                    _config.StopBits
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create serial port: {ex.Message}");
            }
        }

        public void Configure(WeighbridgeConfig config)
        {
            bool wasOpen = _serialPort?.IsOpen == true;
            if (wasOpen)
            {
                Close();
            }

            _config = config;

            try
            {
                _serialPort = new SerialPort(
                    _config.PortName,
                    _config.BaudRate,
                    _config.Parity,
                    _config.DataBits,
                    _config.StopBits
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to configure serial port: {ex.Message}");
            }

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
            try
            {
                if (_serialPort != null && !_serialPort.IsOpen)
                {
                    _serialPort.DataReceived += OnDataReceived;
                    _serialPort.Open();
                    System.Diagnostics.Debug.WriteLine($"Serial port {_config.PortName} opened successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open serial port {_config.PortName}: {ex.Message}");

                // If serial port fails, start simulation for testing
                StartSimulation();
            }
        }

        public void Close()
        {
            try
            {
                StopSimulation();

                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.DataReceived -= OnDataReceived;
                    _serialPort.Close();
                    System.Diagnostics.Debug.WriteLine($"Serial port {_config.PortName} closed");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing serial port: {ex.Message}");
            }
        }

        private void StartSimulation()
        {
            System.Diagnostics.Debug.WriteLine("Starting weight simulation for testing...");
            _simulationTimer = new Timer(SimulateWeightReading, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }

        private void StopSimulation()
        {
            _simulationTimer?.Dispose();
            _simulationTimer = null;
        }

        private void SimulateWeightReading(object state)
        {
            try
            {
                var random = new Random();
                var baseWeight = 25000 + random.Next(-2000, 2000); // Weight around 25000 +/- 2000
                var weight = baseWeight / 100.0; // Convert to proper decimal

                // Simulate some weight data format
                var simulatedData = $"  {weight:F2} KG  ";

                System.Diagnostics.Debug.WriteLine($"Simulating weight data: {simulatedData}");

                // Process as if it came from serial port
                ProcessWeightData(simulatedData);

                // Simulate stability - randomly stable/unstable
                var isStable = random.Next(0, 4) == 0; // 25% chance of being stable
                StabilityChanged?.Invoke(this, isStable);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in weight simulation: {ex.Message}");
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
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
                        ProcessWeightData(line);
                    }
                }
                _serialDataBuffer = new StringBuilder(bufferString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing serial data: {ex.Message}");
            }
        }

        private void ProcessWeightData(string line)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Processing weight data: '{line}'");

                // Fire the raw data event for the settings page preview
                RawDataReceived?.Invoke(this, line);

                var weightReading = _parserService.Parse(line, _config.RegexString);
                if (weightReading != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Parsed weight: {weightReading.Weight} {weightReading.Unit}");

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
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to parse weight from: '{line}' using regex: '{_config.RegexString}'");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing weight data: {ex.Message}");
            }
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
            try
            {
                return SerialPort.GetPortNames();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting available ports: {ex.Message}");
                return new string[] { "COM1", "COM2", "COM3", "COM4" }; // Return some defaults
            }
        }
    }
}