using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DirectShowLib;

namespace WebcamPresetSaver;

public class CameraPropertyCaps
{
    public string Name { get; set; } = string.Empty;
    public int Min { get; set; }
    public int Max { get; set; }
    public int Default { get; set; }
    public int CurrentValue { get; set; }
    public bool IsAuto { get; set; }
    public bool Supported { get; set; }
}

public static class WebcamController
{
    public static readonly Dictionary<string, object> PropertyMapping = new()
    {
        { "Brightness", VideoProcAmpProperty.Brightness },
        { "Contrast", VideoProcAmpProperty.Contrast },
        { "Saturation", VideoProcAmpProperty.Saturation },
        { "Sharpness", VideoProcAmpProperty.Sharpness },
        { "WhiteBalance", VideoProcAmpProperty.WhiteBalance },
        { "Gamma", VideoProcAmpProperty.Gamma },
        { "BacklightComp", VideoProcAmpProperty.BacklightCompensation },
        { "AntiFlickerHertz", (VideoProcAmpProperty)4 }, // Explicit structural COM boundary lock
        { "Exposure", CameraControlProperty.Exposure },
        { "Focus", CameraControlProperty.Focus },
        { "Zoom", CameraControlProperty.Zoom },
        { "Iris", CameraControlProperty.Iris },
        { "Pan", CameraControlProperty.Pan },
        { "Tilt", CameraControlProperty.Tilt },
        { "Roll", CameraControlProperty.Roll }
    };

    [DllImport("oleaut32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int OleCreatePropertyFrame(
        IntPtr hwndOwner, int x, int y, string lpszCaption,
        int cObjects, [MarshalAs(UnmanagedType.Interface)] ref object ppUnk,
        int cPages, IntPtr lpPageClsID, int lcid, int dwReserved, IntPtr lpvReserved);

    public static List<DsDevice> GetVideoDevices() => new(DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice));

    public static void ShowNativeConfigurationWindow(string devicePath, IntPtr parentWindowHandle)
    {
        IBaseFilter? filter = null;
        IFilterGraph2? graph = new FilterGraph() as IFilterGraph2;
        if (graph == null) return;

        try
        {
            var targetDevice = GetDeviceByPath(devicePath);
            if (targetDevice == null || targetDevice.Mon == null) return;

            int hr = graph.AddSourceFilterForMoniker(targetDevice.Mon, null, "WebcamFilter", out filter);
            if (hr < 0 || filter == null) return;

            object filterObj = filter;
            ISpecifyPropertyPages? specPages = filter as ISpecifyPropertyPages;

            if (specPages != null)
            {
                specPages.GetPages(out DsCAUUID cauuid);
                OleCreatePropertyFrame(parentWindowHandle, 0, 0, targetDevice.Name, 1, ref filterObj, cauuid.cElems, cauuid.pElems, 0, 0, IntPtr.Zero);
                Marshal.FreeCoTaskMem(cauuid.pElems);
            }
        }
        catch { }
        finally
        {
            if (filter != null) Marshal.ReleaseComObject(filter);
            Marshal.ReleaseComObject(graph);
        }
    }

    public static List<CameraPropertyCaps> GetDeviceCapabilities(string devicePath)
    {
        var capsList = new List<CameraPropertyCaps>();
        IBaseFilter? filter = null;
        IFilterGraph2? graph = new FilterGraph() as IFilterGraph2;

        if (graph == null) return capsList;

        try
        {
            var targetDevice = GetDeviceByPath(devicePath);
            if (targetDevice == null || targetDevice.Mon == null) return capsList;

            int hr = graph.AddSourceFilterForMoniker(targetDevice.Mon, null, "WebcamFilter", out filter);
            if (hr < 0 || filter == null) return capsList;

            var procAmp = filter as IAMVideoProcAmp;
            var camCtrl = filter as IAMCameraControl;

            foreach (var prop in PropertyMapping)
            {
                var caps = new CameraPropertyCaps { Name = prop.Key };
                int rangeHr = -1, min = 0, max = 0, step = 0, def = 0, current = 0;
                var flags = VideoProcAmpFlags.None;
                var cFlags = CameraControlFlags.None;

                if (prop.Value is VideoProcAmpProperty ampProp && procAmp != null)
                {
                    rangeHr = procAmp.GetRange(ampProp, out min, out max, out step, out def, out flags);
                    if (rangeHr == 0) procAmp.Get(ampProp, out current, out flags);
                }
                else if (prop.Value is CameraControlProperty camProp && camCtrl != null)
                {
                    rangeHr = camCtrl.GetRange(camProp, out min, out max, out step, out def, out cFlags);
                    if (rangeHr == 0) camCtrl.Get(camProp, out current, out cFlags);
                    flags = (VideoProcAmpFlags)cFlags;
                }

                if (rangeHr == 0)
                {
                    caps.Supported = true;
                    caps.Min = min;
                    caps.Max = max;
                    caps.Default = def;
                    caps.CurrentValue = current;
                    caps.IsAuto = (flags == VideoProcAmpFlags.Auto || flags == (VideoProcAmpFlags)CameraControlFlags.Auto);
                    capsList.Add(caps);
                }
            }
        }
        catch { }
        finally 
        { 
            if (filter != null) Marshal.ReleaseComObject(filter); 
            Marshal.ReleaseComObject(graph);
        }
        return capsList;
    }

    public static void SetSingleProperty(string devicePath, string propName, int value, bool isAuto)
    {
        if (!PropertyMapping.TryGetValue(propName, out var propType)) return;
        IBaseFilter? filter = null;
        IFilterGraph2? graph = new FilterGraph() as IFilterGraph2;

        if (graph == null) return;

        try
        {
            var targetDevice = GetDeviceByPath(devicePath);
            if (targetDevice == null || targetDevice.Mon == null) return;

            int hr = graph.AddSourceFilterForMoniker(targetDevice.Mon, null, "WebcamFilter", out filter);
            if (hr < 0 || filter == null) return;

            var flags = isAuto ? VideoProcAmpFlags.Auto : VideoProcAmpFlags.Manual;
            var cFlags = isAuto ? CameraControlFlags.Auto : CameraControlFlags.Manual;

            if (propType is VideoProcAmpProperty ampProp && filter is IAMVideoProcAmp procAmp)
            {
                procAmp.Set(ampProp, value, flags);
            }
            else if (propType is CameraControlProperty camProp && filter is IAMCameraControl camCtrl)
            {
                camCtrl.Set(camProp, value, cFlags);
            }
        }
        catch { }
        finally 
        { 
            if (filter != null) Marshal.ReleaseComObject(filter); 
            Marshal.ReleaseComObject(graph);
        }
    }

    public static Dictionary<string, int> GetCurrentSettings(string devicePath)
    {
        var settings = new Dictionary<string, int>();
        var caps = GetDeviceCapabilities(devicePath);
        foreach (var cap in caps) if (cap.Supported) settings[cap.Name] = cap.CurrentValue;
        return settings;
    }

    public static void ApplySettings(string devicePath, Dictionary<string, int> settings)
    {
        foreach (var s in settings) SetSingleProperty(devicePath, s.Key, s.Value, false);
    }

    private static DsDevice? GetDeviceByPath(string devicePath)
    {
        var devices = GetVideoDevices();
        foreach (var d in devices)
        {
            if (d.DevicePath.Equals(devicePath, StringComparison.OrdinalIgnoreCase)) return d;
        }
        return null;
    }
}
