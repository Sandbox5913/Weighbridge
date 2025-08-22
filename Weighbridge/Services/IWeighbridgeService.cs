using System;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public interface IWeighbridgeService
    {
        event EventHandler<WeightReading> DataReceived;
        event EventHandler<string> RawDataReceived; // Add this line
        event EventHandler<bool> StabilityChanged;
        void Open();
        void Close();
        WeighbridgeConfig GetConfig();
        string[] GetAvailablePorts();
        void Configure(WeighbridgeConfig config);
        bool IsScaleAtZero { get; }
        event EventHandler<bool> ScaleZeroStatusChanged;
    }
}