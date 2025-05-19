using LuminBridgeFramework.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminBridgeFramework
{
    public interface IDeviceController
    {
        bool TryApplyValue(ValueReportPacket packet);
    }
}
