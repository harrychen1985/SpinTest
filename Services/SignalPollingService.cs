using System;
using System.Threading;
using MODBUS_Control_Software.Models;

namespace MODBUS_Control_Software.Services
{
    public class SignalPollingService
    {
        public event Action<bool[]> OnSignalUpdated;
        private readonly ModbusClient _modbus;
        private readonly ModbusSettings _settings;
        private Thread _pollingThread;
        private bool _isRunning;

        public SignalPollingService(ModbusClient modbus, ModbusSettings settings)
        {
            _modbus = modbus;
            _settings = settings;
        }

        public void Start()
        {
            _isRunning = true;
            _pollingThread = new Thread(PollSignals)
            {
                IsBackground = true
            };
            _pollingThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _pollingThread?.Join(500);
        }

        private void PollSignals()
        {
            while (_isRunning && _modbus.IsConnected)
            {
                try
                {
                    var signals = _modbus.ReadCoils(33, 5);
                    OnSignalUpdated?.Invoke(signals);
                }
                catch { /* 忽略通信错误 */ }

                Thread.Sleep(_settings.PollingInterval);
            }
        }
    }
}
