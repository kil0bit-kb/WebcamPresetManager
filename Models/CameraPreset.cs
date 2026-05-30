using System;
using System.Collections.Generic;

namespace WebcamPresetSaver;

public class CameraPreset
{
    public string DeviceName { get; set; } = string.Empty;
    public string DevicePath { get; set; } = string.Empty;
    public Dictionary<string, int> Settings { get; set; } = new Dictionary<string, int>();
}
