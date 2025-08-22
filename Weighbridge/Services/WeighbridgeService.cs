using System.IO.Ports;
using System.Text;
using Weighbridge.Models;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace Weighbridge.Services
{
    public class WeighbridgeService : IWeighbridgeService, IDisposable
    {
        private SerialPort _serialPort;
        private WeighbridgeConfig _config;
        private readonly WeightParserService _parserService;
        private StringBuilder _serialDataBuffer = new StringBuilder();
        private Timer _simulationTimer;
        private bool _disposed = false;
        private readonly object _lock = new object();

        // --- Stability Detection (Thread-Safe) ---
        private readonly List<double> _recentWeights = new List<double>();
        private const int WindowSize = 10;
        private const double Tolerance = 0.5;
        private DateTime? _stabilityStart = null;
        private bool _isSimulationMode = false;
        private bool _isScaleAtZero = true; // Assume true initially
        private WeightReading? _lastWeightReading;
        private bool _currentStabilityStatus = false;

        public bool IsScaleAtZero
        {
            get => _isScaleAtZero;
            private set
            {
                if (_isScaleAtZero != value)
                {
                    _isScaleAtZero = value;
                    ScaleZeroStatusChanged?.Invoke(this, _isScaleAtZero);
                }
            }
        }

        public event EventHandler<WeightReading>? DataReceived;
        public event EventHandler<string>? RawDataReceived;
        public event EventHandler<bool>? StabilityChanged;
        public event EventHandler<bool>? ScaleZeroStatusChanged;

        public bool IsSimulationMode => _isSimulationMode;

        public bool RequireManualZeroConfirmation => _config.RequireManualZeroConfirmation;

        public bool BypassZeroRequirement => _config.BypassZeroRequirement;

        public WeighbridgeService(WeightParserService parserService)
        {
            System.Diagnostics.Debug.WriteLine("WeighbridgeService: Constructor called.");
            System.Diagnostics.Debug.WriteLine($"WeighbridgeService Instance HashCode: {this.GetHashCode()}");
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            _config = LoadConfiguration();
            InitializeSerialPort();
        }

        private WeighbridgeConfig LoadConfiguration()
        {
            try
            {
                var config = new WeighbridgeConfig
                {
                    PortName = Preferences.Get("PortName", "COM1"),
                    BaudRate = ParseIntSafely(Preferences.Get("BaudRate", "9600"), 9600),
                    RegexString = Preferences.Get("RegexString", @"(?<sign>[+-])?(?<num>\d+(?:\.\d+)?)[ ]*(?<unit>[a-zA-Z]{1,4})"),
                    StabilityEnabled = Preferences.Get("StabilityEnabled", true),
                    StableTime = ParseDoubleSafely(Preferences.Get("StableTime", "3.0"), 3.0),
                    StabilityRegex = Preferences.Get("StabilityRegex", ""),
                    
                    ZeroTolerance = ParseDoubleSafely(Preferences.Get("ZeroTolerance", "0.1"), 0.1),
                    RegulatoryZeroTolerance = ParseDoubleSafely(Preferences.Get("RegulatoryZeroTolerance", "0.01"), 0.01),
                    RequireManualZeroConfirmation = Preferences.Get("RequireManualZeroConfirmation", true),
                    BypassZeroRequirement = Preferences.Get("BypassZeroRequirement", false)
                };

                ValidateConfiguration(config);
                return config;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration, using defaults: {ex.Message}");
                return new WeighbridgeConfig(); // Return default configuration
            }
        }

        private void ValidateConfiguration(WeighbridgeConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.PortName))
                throw new ArgumentException("Port name cannot be empty");

            if (config.BaudRate <= 0)
                throw new ArgumentException("Baud rate must be positive");

            if (string.IsNullOrWhiteSpace(config.RegexString))
                throw new ArgumentException("Regex string cannot be empty");

            if (config.StableTime < 0)
                throw new ArgumentException("Stable time cannot be negative");

            // Test regex validity
            try
            {
                _ = new Regex(config.RegexString);
                if (!string.IsNullOrEmpty(config.StabilityRegex))
                {
                    _ = new Regex(config.StabilityRegex);
                }
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Invalid regex pattern: {ex.Message}");
            }
        }

        private int ParseIntSafely(string value, int defaultValue)
        {
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        private double ParseDoubleSafely(string value, double defaultValue)
        {
            return double.TryParse(value, out double result) ? result : defaultValue;
        }

        private double GetDefaultZeroTolerance(string unit)
        {
            // Define default zero tolerances based on unit
            // These values can be adjusted based on typical weighbridge precision
            return unit.ToUpperInvariant() switch
            {
                "KG" => 0.1,
                "LB" => 0.2,
                "T" => 0.0001, // 0.1 kg in tonnes
                _ => 0.1 // Default for unknown units
            };
        }

        private void InitializeSerialPort()
        {
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
                // Don't throw here - allow the service to work in simulation mode
            }
        }

        public void Configure(WeighbridgeConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            ValidateConfiguration(config);

            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(WeighbridgeService));

                bool wasOpen = IsConnected();
                if (wasOpen)
                {
                    Close();
                }

                _config = config;

                // Dispose old serial port safely
                if (_serialPort != null)
                {
                    try
                    {
                        if (_serialPort.IsOpen)
                        {
                            _serialPort.DataReceived -= OnDataReceived;
                            _serialPort.Close();
                        }
                        _serialPort.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error disposing old serial port: {ex.Message}");
                    }
                }

                InitializeSerialPort();

                if (wasOpen)
                {
                    Open();
                }
            }
        }

        public WeighbridgeConfig GetConfig()
        {
            lock (_lock)
            {
                return _config; // Consider returning a copy if immutability is needed
            }
        }

        public bool IsConnected()
        {
            lock (_lock)
            {
                return _serialPort?.IsOpen == true || _isSimulationMode;
            }
        }

        public void Open()
        {
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(WeighbridgeService));

                try
                {
                    if (_serialPort != null && !_serialPort.IsOpen)
                    {
                        _serialPort.DataReceived += OnDataReceived;
                        _serialPort.Open();
                        _isSimulationMode = false;
                        System.Diagnostics.Debug.WriteLine($"Serial port {_config.PortName} opened successfully");
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Serial port {_config.PortName} is already in use: {ex.Message}");
                    StartSimulation();
                }
                catch (ArgumentException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid serial port configuration: {ex.Message}");
                    StartSimulation();
                }
                catch (System.IO.IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Serial port {_config.PortName} not available: {ex.Message}");
                    StartSimulation();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open serial port {_config.PortName}: {ex.Message}");
                    StartSimulation();
                }
            }
        }

        public void Close()
        {
            lock (_lock)
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
                    _isSimulationMode = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error closing serial port: {ex.Message}");
                }
            }
        }

        private void StartSimulation()
        {
            if (_disposed) return;

            System.Diagnostics.Debug.WriteLine("Starting weight simulation for testing...");
            _isSimulationMode = true;

            try
            {
                _simulationTimer = new Timer(SimulateWeightReading, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start simulation: {ex.Message}");
                _isSimulationMode = false;
            }
        }

        private void StopSimulation()
        {
            _simulationTimer?.Dispose();
            _simulationTimer = null;
        }

        private void SimulateWeightReading(object state)
        {
            if (_disposed) return;

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
            if (_disposed) return;

            try
            {
                if (sender is not SerialPort sp) return;

                string indata = sp.ReadExisting();

                lock (_lock)
                {
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing serial data: {ex.Message}");
            }
        }

        private void ProcessWeightData(string line)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessWeightData: Instance HashCode: {this.GetHashCode()}");
            if (_disposed) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Processing weight data: '{line}'");

                // Fire the raw data event for the settings page preview
                RawDataReceived?.Invoke(this, line);

                WeighbridgeConfig configCopy;
                lock (_lock)
                {
                    configCopy = _config; // Get a reference under lock
                }

                var weightReading = _parserService.Parse(line, configCopy.RegexString);
                bool currentIsZero = false;
                bool isStable = false; // Initialize isStable here

                if (weightReading != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Parsed weight: {weightReading.Weight} {weightReading.Unit}");

                    // Fire the parsed data event for the main page
                    DataReceived?.Invoke(this, weightReading);
                    _lastWeightReading = weightReading; // Store the last valid weight reading

                    // Then check numerical zero tolerance.
                    {
                        double zeroTolerance = configCopy.ZeroTolerance > 0 ? configCopy.ZeroTolerance : GetDefaultZeroTolerance(weightReading.Unit);
                        currentIsZero = Math.Abs((double)weightReading.Weight) < zeroTolerance;
                    }

                    // Check for stability
                    if (configCopy.StabilityEnabled) 
                    {
                        if (!string.IsNullOrEmpty(configCopy.StabilityRegex))
                        {
                            isStable = Regex.IsMatch(line, configCopy.StabilityRegex);
                        }
                        else
                        {
                            isStable = IsWeightStable((double)weightReading.Weight, configCopy.StableTime);
                        }
                    }
                }
                else // weightReading is null
                {
                    isStable = false; // Not stable if no valid weight
                }

                IsScaleAtZero = currentIsZero; // Update the property, which will raise the event if changed
                _currentStabilityStatus = isStable; // Update internal stability status
                StabilityChanged?.Invoke(this, isStable); // Always invoke StabilityChanged
            }
            catch (RegexMatchTimeoutException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Regex timeout processing weight data: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid regex processing weight data: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing weight data: {ex.Message}");
            }
        }

        private bool IsWeightStable(double newWeight, double stableTime)
        {
            lock (_lock)
            {
                System.Diagnostics.Debug.WriteLine($"IsWeightStable: Before Add - newWeight={newWeight}, _recentWeights.Count={_recentWeights.Count}");
                _recentWeights.Add(newWeight);
                if (_recentWeights.Count > WindowSize)
                    _recentWeights.RemoveAt(0);

                System.Diagnostics.Debug.WriteLine($"IsWeightStable: Current _recentWeights count: {_recentWeights.Count}");
                if (_recentWeights.Count < WindowSize)
                {
                    System.Diagnostics.Debug.WriteLine($"IsWeightStable: Not enough data yet. Need {WindowSize}, have {_recentWeights.Count}.");
                    return false; // not enough data yet
                }

                double max = double.MinValue, min = double.MaxValue;
                foreach (var w in _recentWeights)
                {
                    if (w > max) max = w;
                    if (w < min) min = w;
                }

                bool inTolerance = (max - min) <= Tolerance;
                System.Diagnostics.Debug.WriteLine($"IsWeightStable: Max: {max}, Min: {min}, Difference: {max - min}, Tolerance: {Tolerance}, InTolerance: {inTolerance}");

                if (inTolerance)
                {
                    if (_stabilityStart == null)
                    {
                        _stabilityStart = DateTime.Now;
                        System.Diagnostics.Debug.WriteLine($"IsWeightStable: Stability timer started at {_stabilityStart.Value}.");
                    }

                    double elapsedTime = (DateTime.Now - _stabilityStart.Value).TotalSeconds;
                    System.Diagnostics.Debug.WriteLine($"IsWeightStable: Elapsed time: {elapsedTime}s, Required stable time: {stableTime}s.");

                    if (elapsedTime >= stableTime)
                    {
                        System.Diagnostics.Debug.WriteLine("IsWeightStable: Scale is stable.");
                        return true;
                    }
                }
                else
                {
                    if (_stabilityStart != null)
                    {
                        System.Diagnostics.Debug.WriteLine("IsWeightStable: Scale became unstable, resetting timer.");
                    }
                    _stabilityStart = null; // reset timer if out of tolerance
                }

                System.Diagnostics.Debug.WriteLine("IsWeightStable: Scale is not stable yet.");
                return false;
            }
        }

        public string[] GetAvailablePorts()
        {
            try
            {
                var ports = SerialPort.GetPortNames();
                return ports.Length > 0 ? ports : new string[] { "COM1", "COM2", "COM3", "COM4" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting available ports: {ex.Message}");
                return new string[] { "COM1", "COM2", "COM3", "COM4" }; // Return some defaults
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                lock (_lock)
                {
                    try
                    {
                        StopSimulation();

                        if (_serialPort != null)
                        {
                            if (_serialPort.IsOpen)
                            {
                                _serialPort.DataReceived -= OnDataReceived;
                                _serialPort.Close();
                            }
                            _serialPort.Dispose();
                            _serialPort = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error during disposal: {ex.Message}");
                    }
                    finally
                    {
                        _disposed = true;
                    }
                }
            }
        }

        public bool PerformZeroOperation()
        {
            lock (_lock)
            {
                System.Diagnostics.Debug.WriteLine($"PerformZeroOperation: Instance HashCode: {this.GetHashCode()}");
                if (_disposed)
                    throw new ObjectDisposedException(nameof(WeighbridgeService));

                if (_lastWeightReading == null)
                {
                    System.Diagnostics.Debug.WriteLine("Zero operation failed: No weight reading available.");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"PerformZeroOperation: Last Weight Reading: {_lastWeightReading.Weight} {_lastWeightReading.Unit}");

                // Check for stability
                // We need to ensure the scale has been stable for the configured StableTime
                // The IsWeightStable method already handles the time component.
                bool isStable = _currentStabilityStatus;
                System.Diagnostics.Debug.WriteLine($"PerformZeroOperation: Is Stable: {isStable}");

                if (!isStable)
                {
                    System.Diagnostics.Debug.WriteLine("Zero operation failed: Scale is not stable.");
                    return false;
                }

                // Check if the current weight is within the regulatory zero tolerance
                double currentWeight = (double)_lastWeightReading.Weight;
                double regulatoryZeroTolerance = _config.RegulatoryZeroTolerance > 0 ? _config.RegulatoryZeroTolerance : GetDefaultZeroTolerance(_lastWeightReading.Unit);
                System.Diagnostics.Debug.WriteLine($"PerformZeroOperation: Regulatory Zero Tolerance: {regulatoryZeroTolerance}");
                System.Diagnostics.Debug.WriteLine($"PerformZeroOperation: Math.Abs(currentWeight) ({Math.Abs(currentWeight)}) < regulatoryZeroTolerance ({regulatoryZeroTolerance}): {Math.Abs(currentWeight) < regulatoryZeroTolerance}");

                if (Math.Abs(currentWeight) < regulatoryZeroTolerance)
                {
                    // Scale is stable and within regulatory zero tolerance
                    System.Diagnostics.Debug.WriteLine($"Zero operation successful. Current weight: {currentWeight} {(_lastWeightReading.Unit ?? "")} within tolerance {regulatoryZeroTolerance}.");
                    // In a real weighbridge, this might trigger a command to the scale to set zero.
                    // For now, we just confirm it conceptually.
                    IsScaleAtZero = true; // Explicitly set to true after successful zero operation
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Zero operation failed: Weight {currentWeight} {(_lastWeightReading.Unit ?? "")} is not within regulatory zero tolerance {regulatoryZeroTolerance}.");
                    return false;
                }
            }
        }

        ~WeighbridgeService()
        {
            Dispose(false);
        }
    }
}