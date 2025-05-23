﻿using System.Text.Json;
using HdrChecker;
using Vortice.DXGI;
using static Vortice.DXGI.DXGI;

internal class Program
{
    static void Main()
    {
        var monitors = new List<HdrMonitorInfo>();

        using IDXGIFactory1 factory = CreateDXGIFactory1<IDXGIFactory1>();

        uint adapterIndex = 0;
        while (factory.EnumAdapters1(adapterIndex++, out IDXGIAdapter1 adapter).Success)
        {
            uint outputIndex = 0;
            while (adapter.EnumOutputs(outputIndex++, out IDXGIOutput output).Success)
            {
                if (output.QueryInterfaceOrNull<IDXGIOutput6>() is IDXGIOutput6 output6)
                {
                    var desc1 = output6.Description1;

                    string deviceName = desc1.DeviceName;
                    bool hdrEnabled = desc1.ColorSpace == ColorSpaceType.RgbFullG2084NoneP2020;

                    monitors.Add(new HdrMonitorInfo
                    {
                        DeviceName = deviceName,
                        HdrEnabled = hdrEnabled,
                        ColorSpace = desc1.ColorSpace.ToString()
                    });
                }
            }
        }

        Console.WriteLine(JsonSerializer.Serialize(monitors, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}
