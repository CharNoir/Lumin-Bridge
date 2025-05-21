using LuminBridgeFramework.Protocol;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LuminBridgeFramework
{
    public class SerialController : IDisposable
    {
        private SerialPort _serialPort;
        private readonly SynchronizationContext _syncContext;

        public event EventHandler<string> OnDataReceived;
        public event EventHandler<string> OnError;
        public event Action<ValueReportPacket> OnValueReportReceived;
        public static Action<BaseDevice> OnVolumeChangedExternally { get; set; }

        public string ConnectedPortName => _serialPort?.PortName;
        public bool IsConnected => _serialPort != null && _serialPort.IsOpen;

        private enum ReceiveState { WaitForHeader, WaitForLength, WaitForPayload }

        private ReceiveState _receiveState = ReceiveState.WaitForHeader;
        private byte[] _rxBuffer = new byte[256];
        private int _expectedLength = 0;
        private int _rxIndex = 0;

        public SerialController()
        {
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            OnVolumeChangedExternally += VolumeChanged;
        }

        private void VolumeChanged(BaseDevice device)
        {
            SendDeltaUpdatePacket(device);
        }

        private void InitPort(string portName, int baudRate)
        {
            _serialPort = new SerialPort(portName, baudRate)
            {
                Encoding = Encoding.ASCII,
                DtrEnable = true,
                RtsEnable = true,
                NewLine = "\n"
            };

            _serialPort.DataReceived += SerialDataReceived;
        }

        public void Connect()
        {
            try
            {
                if (_serialPort != null && !_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Failed to open serial port: {ex.Message}");
            }
        }

        public void SendDeltaUpdatePacket(BaseDevice device)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                RaiseError("[Delta] Serial port not open.");
                return;
            }

            if (!device.IsVisible)
            {
                Console.WriteLine($"[Delta] Skipped invisible device: {device.FriendlyName}");
                return;
            }

            var protocolDevice = device.ToProtocolDevice();
            var deltaPacket = new DeltaUpdatePacket
            {
                packetType = PacketType.DeltaUpdate,
                device = protocolDevice
            };

            var bytes = ProtocolHelper.SerializeDeltaUpdatePacket(deltaPacket);

            try
            {
                _serialPort.Write(new byte[] { 0xAA }, 0, 1);
                _serialPort.Write(new byte[] { (byte)bytes.Length }, 0, 1);
                _serialPort.Write(bytes, 0, bytes.Length);

                Console.WriteLine($"[Delta] Sent: {protocolDevice.deviceType} | ID: {protocolDevice.id} | Name: {protocolDevice.name} | Value: {protocolDevice.value}");
            }
            catch (Exception ex)
            {
                RaiseError($"[Delta] Failed to send packet: {ex.Message}");
            }
        }

        public void SendUpdate(List<BaseDevice> devicesList)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    RaiseError("[Sync] Serial port not open.");
                    return;
                }

                var resetPacket = new byte[]
                {
                    0xAA,
                    0x01,
                    (byte)PacketType.ResetDeviceMatrix
                };

                _serialPort.Write(resetPacket, 0, resetPacket.Length);
                Console.WriteLine("[Sync] Sent ResetDeviceMatrix command");
                Thread.Sleep(10);

                foreach (var device in devicesList)
                {
                    SendDeltaUpdatePacket(device);
                    Thread.Sleep(10);
                }

                Console.WriteLine($"[Sync] Completed sync of {devicesList.Count} devices");
            }
            catch (Exception ex)
            {
                RaiseError($"[Sync] Failed: {ex.Message}");
            }
        }

        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                while (_serialPort.BytesToRead > 0)
                {
                    int read = _serialPort.ReadByte();
                    if (read == -1) return;

                    byte ch = (byte)read;

                    switch (_receiveState)
                    {
                        case ReceiveState.WaitForHeader:
                            if (ch == 0xAA)
                            {
                                _receiveState = ReceiveState.WaitForLength;
                                Console.WriteLine("[Serial] Header detected.");
                            }
                            break;

                        case ReceiveState.WaitForLength:
                            _expectedLength = ch;
                            _rxIndex = 0;
                            if (_expectedLength > _rxBuffer.Length)
                            {
                                Console.WriteLine("[Serial] Packet too large. Ignored.");
                                _receiveState = ReceiveState.WaitForHeader;
                            }
                            else
                            {
                                _receiveState = ReceiveState.WaitForPayload;
                            }
                            break;

                        case ReceiveState.WaitForPayload:
                            _rxBuffer[_rxIndex++] = ch;
                            if (_rxIndex == _expectedLength)
                            {
                                _receiveState = ReceiveState.WaitForHeader;
                                HandleBinaryPacket(_rxBuffer, _expectedLength);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Error reading data: {ex.Message}");
            }
        }

        private void HandleBinaryPacket(byte[] buffer, int length)
        {
            if (length < 1)
            {
                Console.WriteLine("[Serial] Empty packet received.");
                return;
            }

            var packetType = (PacketType)buffer[0];

            switch (packetType)
            {
                case PacketType.ValueReport:
                    if (length == Marshal.SizeOf<ValueReportPacket>())
                    {
                        ValueReportPacket packet = ProtocolHelper.BytesToStructure<ValueReportPacket>(buffer);
                        Console.WriteLine($"[ValueReport] id={packet.id} value={packet.value} type={packet.deviceType}");

                        _syncContext.Post(_ => OnValueReportReceived?.Invoke(packet), null);
                    }
                    else
                    {
                        Console.WriteLine("[ValueReport] Invalid packet size.");
                    }
                    break;

                default:
                    Console.WriteLine($"[Serial] Unknown packet type: {packetType}");
                    break;
            }
        }

        private void RaiseError(string message)
        {
            _syncContext.Post(_ => OnError?.Invoke(this, message), null);
        }

        /// <summary>
        /// Scans all COM ports, sends a handshake message, and connects to the one that responds correctly.
        /// </summary>
        /// <param name="handshakeMessage">Message sent to identify the device (default: HELLO_LUMIN).</param>
        /// <param name="expectedResponse">Expected reply from the correct device (default: LUMIN_ACK).</param>
        /// <param name="baudRate">Baud rate to use during connection.</param>
        /// <param name="timeoutMs">Timeout for read/write operations in milliseconds.</param>
        /// <returns>True if successfully connected to the Lumin device; false otherwise.</returns>
        public bool IdentifyAndConnect(
            string handshakeMessage = "HELLO_LUMIN",
            string expectedResponse = "LUMIN_ACK",
            int baudRate = 115200,
            int timeoutMs = 100)
        {
            Console.WriteLine("Scanning available COM ports for Lumin device...");

            string matchedPort = null;

            foreach (var portName in SerialPort.GetPortNames())
            {
                Console.WriteLine($"Trying port {portName}...");

                try
                {
                    using (var probePort = new SerialPort(portName, baudRate)
                    {
                        ReadTimeout = timeoutMs,
                        WriteTimeout = timeoutMs,
                        Encoding = Encoding.ASCII,
                        DtrEnable = true,
                        RtsEnable = true,
                        NewLine = "\n"
                    })
                    {
                        probePort.Open();
                        Console.WriteLine($"Opened port {portName}");

                        probePort.DiscardInBuffer();
                        probePort.DiscardOutBuffer();

                        probePort.WriteLine(handshakeMessage);
                        Console.WriteLine($"Sent handshake '{handshakeMessage}' to {portName}");

                        string response = probePort.ReadLine().Trim();
                        Console.WriteLine($"Received response from {portName}: '{response}'");

                        if (response == expectedResponse)
                        {
                            matchedPort = portName;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to connect on {portName}: {ex.Message}");
                }
            }

            if (matchedPort != null)
            {
                Console.WriteLine($"Matched expected response on {matchedPort}. Connecting...");

                Dispose();
                InitPort(matchedPort, baudRate);
                Connect();

                if (_serialPort != null && _serialPort.IsOpen)
                {
                    Console.WriteLine($"Successfully connected to {matchedPort}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to open {matchedPort} after matching response.");
                }
            }

            Console.WriteLine("Lumin device not found on any available COM port.");
            return false;
        }

        public void Dispose()
        {
            if (_serialPort != null)
            {
                try
                {
                    _serialPort.DataReceived -= SerialDataReceived;
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }
                    _serialPort.Dispose();
                }
                catch { }
                _serialPort = null;
            }
        }
    }
}
