using System;

namespace MODBUS_Control_Software.Models
{
    public class ModbusSettings
    {
        public string IPAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 502;
        public int PollingInterval { get; set; } = 500;
    }
}
