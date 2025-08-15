using System;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public interface IWeighbridgeService
    {
        event EventHandler<WeightReading> DataReceived;
        event EventHandler<bool> StabilityChanged;
        void Open();
        void Close();
        WeighbridgeConfig GetConfig();
    }
}
