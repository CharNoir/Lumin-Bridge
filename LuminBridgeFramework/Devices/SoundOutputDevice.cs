using LuminBridgeFramework.Protocol;
using NAudio.CoreAudioApi;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Xml.Linq;

namespace LuminBridgeFramework
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SoundOutputDevice : BaseDevice
    {
        public MMDevice Device { get; private set; }
        public event Action<SoundOutputDevice> VolumeChangedExternally;

        public SoundOutputDevice(MMDevice device)
        {
            Device = device;
            FriendlyName = device.FriendlyName;
            Device.AudioEndpointVolume.OnVolumeNotification += VolumeChanged;
        }

        public SoundOutputDevice(){}

        public int GetVolume()
        {
            return (int)Math.Round(Device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
        }

        public void SetVolume(float level)
        {
            level = MathHelper.Clamp(level, 0.0f, 1.0f);
            float currentValue = Device.AudioEndpointVolume.MasterVolumeLevelScalar;
            if (level == 0)
            {
                Device.AudioEndpointVolume.Mute = true;
                return;
            }
            else if (Device.AudioEndpointVolume.Mute)
            {
                Device.AudioEndpointVolume.Mute = false;
            }
            Device.AudioEndpointVolume.MasterVolumeLevelScalar = level;
        }

        private void VolumeChanged(AudioVolumeNotificationData data)
        {
            //Console.WriteLine($"Volume changed externaly: {data.MasterVolume}, {Device.AudioEndpointVolume.MasterVolumeLevelScalar}");
            VolumeChangedExternally?.Invoke(this);
        }

        public override Device ToProtocolDevice()
        {
            byte volume = (byte)MathHelper.Clamp(GetVolume(), 0, 100);
            if (Device.AudioEndpointVolume.Mute) { volume = 0; }

            return new Device
            {
                name = SerializationHelper.AsciiStringToFixedLengthString(FriendlyName, 16),
                id = (byte)IconId,
                value = volume,
                deviceType = DeviceType.Volume
            };
        }

        public override void SaveConfig()
        {
            string path = Path.Combine(SerializationHelper.ConfigDirectory, $"{SerializationHelper.MakeSafeFileName(Device.ID)}.json");

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver
                {
                    IgnoreSerializableAttribute = true
                },
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            File.WriteAllText(path, JsonConvert.SerializeObject(this, settings));
        }

        public void LoadConfig()
        {
            string path = Path.Combine(SerializationHelper.ConfigDirectory,
                $"{SerializationHelper.MakeSafeFileName(Device.ID)}.json");

            if (!File.Exists(path)) return;

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    IgnoreSerializableAttribute = true
                },
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            try
            {
                var json = File.ReadAllText(path);
                var config = JsonConvert.DeserializeObject<SoundOutputDevice>(json, settings);
                if (!string.IsNullOrWhiteSpace(config?.FriendlyName))
                {
                    FriendlyName = config.FriendlyName;
                    IsVisible = config.IsVisible;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Failed to load config for device {Device.ID}: {ex.Message}");
            }
        }
    }
}
