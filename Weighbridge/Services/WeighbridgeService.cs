
using System.IO.Ports;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public class WeighbridgeService
    {
        private SerialPort _serialPort;
        private WeighbridgeConfig _config;

        public event EventHandler<string>? DataReceived;

        public WeighbridgeService()
        {
            _config = new WeighbridgeConfig();
            _serialPort = new SerialPort();
        }

        public void Configure(WeighbridgeConfig config)
        {
            _config = config;
            _serialPort.PortName = _config.PortName;
            _serialPort.BaudRate = _config.BaudRate;
            _serialPort.Parity = _config.Parity;
            _serialPort.DataBits = _config.DataBits;
            _serialPort.StopBits = _config.StopBits;
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
            DataReceived?.Invoke(this, indata);
        }

        public string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }
    }
}
