using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuminBridgeFramework.Protocol;
using Newtonsoft.Json;

namespace LuminBridgeFramework
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseDevice : IDevice
    {
        [JsonProperty("friendlyName")]
        public string FriendlyName { get; set; }
        [JsonProperty("visible")]
        public bool IsVisible { get; set; }

        public int IconId { get; set; }

        public abstract Device ToProtocolDevice();
        public abstract void SaveConfig();
    }
}
