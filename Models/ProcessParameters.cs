namespace MODBUS_Control_Software.Models
{
    public class ProcessParameters
    {
        public ushort RunProcessID { get; set; }
        public ushort EditID { get; set; }
        public ushort DIWSpeed { get; set; }
        public ushort DIWTime { get; set; }
        public ushort N2Speed { get; set; }
        public ushort N2Time { get; set; }

        public ushort[] ToRegisterArray() => 
            new ushort[] { RunProcessID, EditID, DIWSpeed, DIWTime, N2Speed, N2Time };
    }
}
