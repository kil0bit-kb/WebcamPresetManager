using System;
using System.Windows.Forms;

namespace WebcamPresetSaver;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.ThreadException += (sender, e) => {
            MessageBox.Show($"UI Thread Error: {e.Exception.Message}", "Crash Caught", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
            var ex = (Exception)e.ExceptionObject;
            MessageBox.Show($"Core App Error: {ex.Message}", "App Crash", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        Application.Run(new Form1());
    }
}
