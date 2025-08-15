using System;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public interface IWeighbridgeService
    {
        event EventHandler<WeightReading> DataReceived;
        event EventHandler<string> RawDataReceived; // Add this event
        event EventHandler<bool> StabilityChanged;
        void Open();
        void Close();
        WeighbridgeConfig GetConfig();
        string[] GetAvailablePorts(); // Add this method
        void Configure(WeighbridgeConfig config); // Add this method
    }
}