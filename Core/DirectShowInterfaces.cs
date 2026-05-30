using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace WebcamPresetManager.Core;

// --- GUID Definitions for DirectShow COM Interfaces ---

// {42103B7E-A5C6-11D0-8E0F-00AA00303A22}
[Guid("42103B7E-A5C6-11D0-8E0F-00AA00303A22")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ICreateDevEnum
{
    // Function declarations for Discovering Devices
}

// {984A7365-82C7-11D0-8E0F-00AA00303A22}
[Guid("984A7365-82C7-11D0-8E0F-00AA00303A22")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IEnumMoniker
{
    // Function declarations for Enumerating Devices
}

// {A5CC917A-D46C-11D0-8E0F-00AA00303A22}
[Guid("A5CC917A-D46C-11D0-8E0F-00AA00303A22")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMonitor
{
    // Function declarations for Monitor information
}

// {B95CD782-4C21-11D0-8E0F-00AA00303A22}
[Guid("B95CD782-4C21-11D0-8E0F-00AA00303A22")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IBaseFilter
{
    // Function declarations for base filter capabilities (e.g., get stream)
}

// {94B157A7-6DCC-11D0-8E0F-00AA00303A22}
[Guid("94B157A7-6DCC-11D0-8E0F-00AA00303A22")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAMVideoProcAmp
{
    // Property definitions and methods for Video Processing Amplifiers
    enum VideoProcAmpProperty
    {
        Brightness = 0,
        Contrast = 1,
        Saturation = 2,
        Sharpness = 3,
        Gamma = 4,
        WhiteBalance = 5
        // Add more as needed
    }

    // Methods for getting and setting properties
    int Get(VideoProcAmpProperty property, out int value);
    void Set(VideoProcAmpProperty property, int value);
}

// {A7C1398B-4F02-11D0-8E0F-00AA00303A22}
[Guid("A7C1398B-4F02-11D0-8E0F-00AA00303A22")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAMCameraControl
{
    // Property definitions and methods for Camera Controls
    enum CameraControlProperty
    {
        Pan = 0,
        Tilt = 1,
        Roll = 2,
        Zoom = 3,
        Exposure = 4,
        Iris = 5,
        Focus = 6
        // Add more as needed
    }

    int Get(CameraControlProperty property, out int value);
    void Set(CameraControlProperty property, int value);
}