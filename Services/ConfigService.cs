using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace WebcamPresetSaver.Services;

public static class ConfigService
{
    // ✅ Uses custom .ccp extension to avoid scanning system files
    public static string? SavePresetDialog(CameraPreset preset)
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "Webcam Preset Profile (*.ccp)|*.ccp",
            DefaultExt = "ccp",
            Title = "Save Webcam Preset Profile",
            InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(preset, options);
                File.WriteAllText(sfd.FileName, jsonString);
                return sfd.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save profile: {ex.Message}", "File Write Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        return null;
    }

    public static CameraPreset? LoadPresetDialog()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Webcam Preset Profile (*.ccp)|*.ccp",
            Title = "Select Webcam Preset Profile",
            InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                string jsonString = File.ReadAllText(ofd.FileName);
                return JsonSerializer.Deserialize<CameraPreset>(jsonString);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read profile: {ex.Message}", "File Read Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        return null;
    }
    
    public static CameraPreset? LoadPresetFromPath(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            string jsonString = File.ReadAllText(path);
            return JsonSerializer.Deserialize<CameraPreset>(jsonString);
        }
        catch { return null; }
    }
}
