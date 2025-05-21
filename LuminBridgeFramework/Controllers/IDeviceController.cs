using LuminBridgeFramework.Protocol;
using System.Collections.Generic;

namespace LuminBridgeFramework
{
    public interface IDeviceController
    {
        /// <summary>
        /// Attempts to apply a value update from a <see cref="ValueReportPacket"/> to the appropriate device.
        /// </summary>
        /// <param name="packet">The packet containing the new value and device information.</param>
        /// <returns>
        /// True if the value was successfully applied to a matching device; otherwise, false.
        /// </returns>
        bool TryApplyValue(ValueReportPacket packet);
        /// <summary>
        /// Retrieves the list of devices managed by this controller.
        /// </summary>
        /// <returns>A list of devices derived from <see cref="BaseDevice"/>.</returns>
        List<BaseDevice> GetDevices();
    }
}
