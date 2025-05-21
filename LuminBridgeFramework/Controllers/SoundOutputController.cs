using LuminBridgeFramework.Protocol;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LuminBridgeFramework
{
    public class SoundOutputController : IDeviceController
    {
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly List<SoundOutputDevice> _outputDevices;
        public List<BaseDevice> GetDevices() => _outputDevices.Cast<BaseDevice>().ToList();
        public SoundOutputController()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _outputDevices = new List<SoundOutputDevice>();

            int index = 0;
            foreach (var device in _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                var soundDevice = new SoundOutputDevice(device) { IconId = index++ };
                soundDevice.LoadConfig();
                _outputDevices.Add(soundDevice);
            }
        }

        // ───────────────────────────────────────────────────────────────
        // 🔷 Public Methods
        // ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Lists all active output device names.
        /// </summary>
        public List<string> ListDeviceNames()
        {
            var names = new List<string>();
            foreach (var device in _outputDevices)
            {
                names.Add(device.FriendlyName);
            }
            return names;
        }

        /// <summary>
        /// Gets the current volume of a device by name.
        /// </summary>
        public float? GetVolume(string deviceName)
        {
            //var device = _outputDevices.Find(d => d.FriendlyName == deviceName);
            //return device?.AudioEndpointVolume.MasterVolumeLevelScalar; // 0.0 to 1.0
            return _outputDevices.Find(d => d.FriendlyName == deviceName)?.GetVolume();
        }

        /// <summary>
        /// Sets the volume of a device by name.
        /// </summary>
        public bool SetVolume(string deviceName, float volumeLevel)
        {
            var device = _outputDevices.Find(d => d.FriendlyName == deviceName);
            if (device == null) return false;

            device.SetVolume(volumeLevel);
            return true;
        }

        /// <summary>
        /// Gets the default output device.
        /// </summary>
        public string GetDefaultDeviceName()
        {
            var defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return defaultDevice?.FriendlyName;
        }

        public bool TryApplyValue(ValueReportPacket packet)
        {
            if (packet.deviceType != DeviceType.Volume)
                return false;

            var device = _outputDevices.FirstOrDefault(d => d.IconId == packet.id);

            if (device == null) return false;

            device.SetVolume(MathHelper.Clamp(packet.value, 0, 100) / 100.0f);
            Console.WriteLine($"[VolumeController] Set volume {packet.value} for {device.FriendlyName}");
            return true;
        }
    }
}
