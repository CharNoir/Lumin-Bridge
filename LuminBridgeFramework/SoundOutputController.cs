using LuminBridgeFramework.Protocol;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminBridgeFramework
{
    public class SoundOutputController : IDeviceController
    {
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly List<MMDevice> _outputDevices;

        public SoundOutputController()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _outputDevices = new List<MMDevice>();

            foreach (var device in _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                _outputDevices.Add(device);
            }
        }

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
            var device = _outputDevices.Find(d => d.FriendlyName == deviceName);
            return device?.AudioEndpointVolume.MasterVolumeLevelScalar; // 0.0 to 1.0
        }

        /// <summary>
        /// Sets the volume of a device by name.
        /// </summary>
        public bool SetVolume(string deviceName, float volumeLevel)
        {
            var device = _outputDevices.Find(d => d.FriendlyName == deviceName);
            if (device != null)
            {
                volumeLevel = Math.Max(0.0f, Math.Min(1.0f, volumeLevel));
                device.AudioEndpointVolume.MasterVolumeLevelScalar = volumeLevel;
                return true;
            }
            return false;
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
            throw new NotImplementedException();
        }
    }
}
