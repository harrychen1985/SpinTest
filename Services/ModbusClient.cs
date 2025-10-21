using System;
using System.Net.Sockets;
using System.Threading;

public class ModbusClient : IDisposable
{
    private TcpClient _tcpClient;
    private NetworkStream _stream;
    private string _ipAddress;
    private int _port;
    private bool _isConnected;

    public bool IsConnected => _isConnected;

    public ModbusClient(string ipAddress, int port)
    {
        _ipAddress = ipAddress;
        _port = port;
    }

    public bool Connect()
    {
        try
        {
            _tcpClient = new TcpClient(_ipAddress, _port);
            _stream = _tcpClient.GetStream();
            _isConnected = true;
            return true;
        }
        catch
        {
            _isConnected = false;
            return false;
        }
    }

    public void Disconnect()
    {
        _stream?.Close();
        _tcpClient?.Close();
        _isConnected = false;
    }

    public bool WriteSingleCoil(ushort address, bool value)
    {
        if (!_isConnected) return false;

        try
        {
            byte[] request = new byte[12];
            // Transaction Identifier
            request[0] = 0x00;
            request[1] = 0x01;
            // Protocol Identifier
            request[2] = 0x00;
            request[3] = 0x00;
            // Length
            request[4] = 0x00;
            request[5] = 0x06;
            // Unit Identifier
            request[6] = 0x01;
            // Function Code
            request[7] = 0x05;
            // Address
            request[8] = (byte)(address >> 8);
            request[9] = (byte)(address & 0xFF);
            // Value
            request[10] = value ? (byte)0xFF : (byte)0x00;
            request[11] = 0x00;

            _stream.Write(request, 0, request.Length);

            byte[] response = new byte[12];
            _stream.Read(response, 0, response.Length);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool WriteMultipleRegisters(ushort startAddress, ushort[] values)
    {
        if (!_isConnected) return false;

        try
        {
            byte[] request = new byte[13 + values.Length * 2];
            // Transaction Identifier
            request[0] = 0x00;
            request[1] = 0x01;
            // Protocol Identifier
            request[2] = 0x00;
            request[3] = 0x00;
            // Length
            request[4] = (byte)(7 + values.Length * 2 >> 8);
            request[5] = (byte)(7 + values.Length * 2 & 0xFF);
            // Unit Identifier
            request[6] = 0x01;
            // Function Code
            request[7] = 0x10;
            // Start Address
            request[8] = (byte)(startAddress >> 8);
            request[9] = (byte)(startAddress & 0xFF);
            // Quantity
            request[10] = (byte)(values.Length >> 8);
            request[11] = (byte)(values.Length & 0xFF);
            // Byte Count
            request[12] = (byte)(values.Length * 2);

            for (int i = 0; i < values.Length; i++)
            {
                request[13 + i * 2] = (byte)(values[i] >> 8);
                request[14 + i * 2] = (byte)(values[i] & 0xFF);
            }

            _stream.Write(request, 0, request.Length);

            byte[] response = new byte[12];
            _stream.Read(response, 0, response.Length);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool[] ReadCoils(ushort startAddress, ushort quantity)
    {
        if (!_isConnected) return null;

        try
        {
            byte[] request = new byte[12];
            // Transaction Identifier
            request[0] = 0x00;
            request[1] = 0x01;
            // Protocol Identifier
            request[2] = 0x00;
            request[3] = 0x00;
            // Length
            request[4] = 0x00;
            request[5] = 0x06;
            // Unit Identifier
            request[6] = 0x01;
            // Function Code
            request[7] = 0x01;
            // Start Address
            request[8] = (byte)(startAddress >> 8);
            request[9] = (byte)(startAddress & 0xFF);
            // Quantity
            request[10] = (byte)(quantity >> 8);
            request[11] = (byte)(quantity & 0xFF);

            _stream.Write(request, 0, request.Length);

            byte[] response = new byte[5 + (int)Math.Ceiling(quantity / 8.0)];
            _stream.Read(response, 0, response.Length);

            bool[] results = new bool[quantity];
            for (int i = 0; i < quantity; i++)
            {
                int byteIndex = i / 8;
                int bitIndex = i % 8;
                results[i] = (response[3 + byteIndex] & (1 << bitIndex)) != 0;
            }

            return results;
        }
        catch
        {
            return null;
        }
    }

    public ushort[] ReadHoldingRegisters(ushort startAddress, ushort quantity)
    {
        if (!_isConnected) return null;

        try
        {
            byte[] request = new byte[12];
            // Transaction Identifier
            request[0] = 0x00;
            request[1] = 0x01;
            // Protocol Identifier
            request[2] = 0x00;
            request[3] = 0x00;
            // Length
            request[4] = 0x00;
            request[5] = 0x06;
            // Unit Identifier
            request[6] = 0x01;
            // Function Code
            request[7] = 0x03;
            // Start Address
            request[8] = (byte)(startAddress >> 8);
            request[9] = (byte)(startAddress & 0xFF);
            // Quantity
            request[10] = (byte)(quantity >> 8);
            request[11] = (byte)(quantity & 0xFF);

            _stream.Write(request, 0, request.Length);

            byte[] response = new byte[5 + quantity * 2];
            _stream.Read(response, 0, response.Length);

            ushort[] results = new ushort[quantity];
            for (int i = 0; i < quantity; i++)
            {
                results[i] = (ushort)((response[3 + i * 2] << 8) | response[4 + i * 2]);
            }

            return results;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}
