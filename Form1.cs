using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DirectShowLib;
using Microsoft.Win32;
using WebcamPresetSaver.Services;

namespace WebcamPresetSaver;

public partial class Form1 : Form
{
    private ComboBox comboDevices = null!;
    private ComboBox comboPresets = null!;
    private Button btnRefresh = null!, btnSaveAs = null!, btnSaveExisting = null!, btnBrowseLoad = null!, btnAdvanced = null!;
    private CheckBox chkRunAtLogin = null!;
    private Panel controlsPanel = null!;
    private NotifyIcon notifyIcon1 = null!;
    private System.Windows.Forms.Timer syncTimer = null!;
    private List<DsDevice> webcams = new();
    private Dictionary<string, string> trackedPresets = new();
    private bool isUpdatingUI = false;

    public Form1()
    {
        InitializeDarkUI();
        InitializePollingLoop();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        RefreshHardwareTree();
        ScanLocalProfileDirectory();
        CheckRegistryStartup();

        string[] args = Environment.GetCommandLineArgs();
        foreach (string arg in args)
        {
            if (arg.Equals("--silent", StringComparison.OrdinalIgnoreCase))
            {
                this.BeginInvoke(new Action(() => {
                    this.Hide(); this.WindowState = FormWindowState.Minimized; this.ShowInTaskbar = false;
                    ApplyFallbackStartupPreset();
                }));
                break;
            }
        }
    }

    private void InitializeDarkUI()
    {
        this.Text = "Webcam Master Control Hub"; this.Size = new Size(560, 740);
        this.BackColor = Color.FromArgb(18, 18, 18); this.ForeColor = Color.FromArgb(240, 240, 240);
        this.FormBorderStyle = FormBorderStyle.FixedSingle; this.MaximizeBox = false; this.StartPosition = FormStartPosition.CenterScreen;

        notifyIcon1 = new NotifyIcon { Icon = SystemIcons.Application, Text = "Webcam Hardware Hook Active" };
        notifyIcon1.MouseDoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; this.ShowInTaskbar = true; notifyIcon1.Visible = false; };

        Label lblDevice = new Label { Text = "VIDEO INPUT SOURCE:", Location = new Point(20, 15), Size = new Size(200, 20), Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(150, 150, 150) };
        comboDevices = new ComboBox { Location = new Point(20, 38), Size = new Size(370, 28), BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9) };
        comboDevices.SelectedIndexChanged += (s, e) => BuildDynamicSliders();
        
        btnRefresh = new Button { Text = "🔄 Refresh Hardware", Location = new Point(400, 37), Size = new Size(130, 28), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9) };
        btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70); btnRefresh.Click += (s, e) => RefreshHardwareTree();

        controlsPanel = new Panel { Location = new Point(20, 80), Size = new Size(510, 380), BackColor = Color.FromArgb(26, 26, 26), AutoScroll = true };

        Label lblPresets = new Label { Text = "PROFILE MANAGEMENT SYSTEM:", Location = new Point(20, 480), Size = new Size(250, 20), Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(150, 150, 150) };
        comboPresets = new ComboBox { Location = new Point(20, 502), Size = new Size(240, 28), BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9) };
        comboPresets.SelectedIndexChanged += comboPresets_SelectedIndexChanged;

        btnSaveAs = new Button { Text = "💾 Save As New Profile", Location = new Point(275, 501), Size = new Size(120, 29), BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8, FontStyle.Bold) };
        btnSaveAs.FlatAppearance.BorderSize = 0; btnSaveAs.Click += btnSaveAs_Click;

        btnSaveExisting = new Button { Text = "Overwrite Selected", Location = new Point(405, 501), Size = new Size(125, 29), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8) };
        btnSaveExisting.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70); btnSaveExisting.Click += btnSaveExisting_Click;

        btnBrowseLoad = new Button { Text = "📂 Load Custom Profile File...", Location = new Point(20, 545), Size = new Size(240, 32), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9) };
        btnBrowseLoad.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70); btnBrowseLoad.Click += btnBrowseLoad_Click;

        btnAdvanced = new Button { Text = "🛠️ Open Windows Native Setup", Location = new Point(275, 545), Size = new Size(255, 32), BackColor = Color.FromArgb(100, 30, 150), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        btnAdvanced.FlatAppearance.BorderSize = 0; btnAdvanced.Click += btnAdvanced_Click;

        chkRunAtLogin = new CheckBox { Text = "Automatically apply fallback configuration profile at system user login", Location = new Point(20, 600), Size = new Size(510, 30), Font = new Font("Segoe UI", 9), FlatStyle = FlatStyle.Flat };
        chkRunAtLogin.CheckedChanged += chkRunAtLogin_CheckedChanged;

        this.Controls.AddRange(new Control[] { lblDevice, comboDevices, btnRefresh, controlsPanel, lblPresets, comboPresets, btnSaveAs, btnSaveExisting, btnBrowseLoad, btnAdvanced, chkRunAtLogin });
    }

    private void InitializePollingLoop()
    {
        syncTimer = new System.Windows.Forms.Timer { Interval = 1500 };
        syncTimer.Tick += (s, e) => PollHardwareAdjustments();
        syncTimer.Start();
    }

    private void RefreshHardwareTree()
    {
        webcams = WebcamController.GetVideoDevices(); comboDevices.Items.Clear();
        foreach (var d in webcams) comboDevices.Items.Add(d.Name);
        if (comboDevices.Items.Count > 0) comboDevices.SelectedIndex = 0; else controlsPanel.Controls.Clear();
    }

    private void ScanLocalProfileDirectory()
    {
        comboPresets.Items.Clear(); trackedPresets.Clear();
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        string[] files = Directory.GetFiles(dir, "*.ccp");

        foreach (var file in files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (name.Equals("webcam_preset", StringComparison.OrdinalIgnoreCase)) continue;
            comboPresets.Items.Add(name); trackedPresets[name] = file;
        }
    }
    private void BuildDynamicSliders()
    {
        if (comboDevices.SelectedIndex == -1 || webcams.Count == 0) return;
        controlsPanel.Controls.Clear(); isUpdatingUI = true;
        try
        {
            var selectedDevice = webcams[comboDevices.SelectedIndex];
            var capabilities = WebcamController.GetDeviceCapabilities(selectedDevice.DevicePath);
            int yOffset = 15;
            foreach (var cap in capabilities)
            {
                if (!cap.Supported) continue;
                Label lblName = new Label { Text = cap.Name == "AntiFlickerHertz" ? "Anti Flicker" : cap.Name, Location = new Point(15, yOffset), Size = new Size(110, 20), Font = new Font("Segoe UI", 9), ForeColor = Color.DarkGray };

                // ✅ Fix: Check if property is AntiFlickerHertz and replace trackbar with a Dropdown ComboBox
                if (cap.Name == "AntiFlickerHertz")
                {
                    ComboBox comboHz = new ComboBox { Location = new Point(130, yOffset - 3), Size = new Size(200, 25), BackColor = Color.FromArgb(32, 32, 32), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Name = "combo_AntiFlickerHertz" };
                    comboHz.Items.AddRange(new string[] { "Disabled", "50 Hz", "60 Hz" });
                    comboHz.SelectedIndex = Math.Clamp(cap.CurrentValue, 0, 2);
                    comboHz.SelectedIndexChanged += (s, e) => {
                        if (!isUpdatingUI) WebcamController.SetSingleProperty(selectedDevice.DevicePath, "AntiFlickerHertz", comboHz.SelectedIndex, false);
                    };
                    controlsPanel.Controls.AddRange(new Control[] { lblName, comboHz });
                }
                else
                {
                    TrackBar slider = new TrackBar { Location = new Point(130, yOffset - 5), Size = new Size(200, 35), Minimum = cap.Min, Maximum = cap.Max, Value = Math.Clamp(cap.CurrentValue, cap.Min, cap.Max), TickStyle = TickStyle.None, BackColor = Color.FromArgb(26, 26, 26), Name = $"track_{cap.Name}" };
                    Label lblVal = new Label { Text = slider.Value.ToString(), Location = new Point(335, yOffset), Size = new Size(65, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold), Name = $"val_{cap.Name}" };
                    CheckBox chkAuto = new CheckBox { Text = "Auto", Location = new Point(410, yOffset - 2), Size = new Size(60, 20), Checked = cap.IsAuto, FlatStyle = FlatStyle.Flat, Name = $"auto_{cap.Name}" };
                    slider.Enabled = !chkAuto.Checked;

                    slider.Scroll += (s, e) => { lblVal.Text = slider.Value.ToString(); if (!isUpdatingUI) WebcamController.SetSingleProperty(selectedDevice.DevicePath, cap.Name, slider.Value, chkAuto.Checked); };
                    chkAuto.CheckedChanged += (s, e) => { slider.Enabled = !chkAuto.Checked; if (!isUpdatingUI) WebcamController.SetSingleProperty(selectedDevice.DevicePath, cap.Name, slider.Value, chkAuto.Checked); };

                    controlsPanel.Controls.AddRange(new Control[] { lblName, slider, lblVal, chkAuto });
                }
                yOffset += 45;
            }
        }
        catch { }
        finally { isUpdatingUI = false; }
    }

    private void PollHardwareAdjustments()
    {
        if (isUpdatingUI || comboDevices.SelectedIndex == -1 || webcams.Count == 0) return;
        try
        {
            var selectedDevice = webcams[comboDevices.SelectedIndex];
            var capabilities = WebcamController.GetDeviceCapabilities(selectedDevice.DevicePath);
            foreach (var cap in capabilities)
            {
                if (!cap.Supported) continue;

                if (cap.Name == "AntiFlickerHertz")
                {
                    var cmbArr = controlsPanel.Controls.Find("combo_AntiFlickerHertz", true);
                    if (cmbArr.Length > 0 && cmbArr[0] is ComboBox comboHz && !comboHz.Focused)
                    {
                        comboHz.SelectedIndex = Math.Clamp(cap.CurrentValue, 0, 2);
                    }
                }
                else
                {
                    var trackArr = controlsPanel.Controls.Find($"track_{cap.Name}", true);
                    var labelArr = controlsPanel.Controls.Find($"val_{cap.Name}", true);
                    var autoArr = controlsPanel.Controls.Find($"auto_{cap.Name}", true);

                    if (trackArr.Length > 0 && trackArr[0] is TrackBar slider && !slider.Focused) slider.Value = Math.Clamp(cap.CurrentValue, cap.Min, cap.Max);
                    if (labelArr.Length > 0 && labelArr[0] is Label lbl) lbl.Text = cap.CurrentValue.ToString();
                    if (autoArr.Length > 0 && autoArr[0] is CheckBox chk) { chk.Checked = cap.IsAuto; if (trackArr.Length > 0) trackArr[0].Enabled = !cap.IsAuto; }
                }
            }
        }
        catch { }
    }

    private void btnSaveAs_Click(object? sender, EventArgs e)
    {
        if (comboDevices.SelectedIndex == -1 || webcams.Count == 0) return;
        var selectedDevice = webcams[comboDevices.SelectedIndex];
        var settings = WebcamController.GetCurrentSettings(selectedDevice.DevicePath);
        var preset = new CameraPreset { DeviceName = selectedDevice.Name, DevicePath = selectedDevice.DevicePath, Settings = settings };
        string? savedPath = ConfigService.SavePresetDialog(preset);
        if (savedPath != null)
        {
            string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webcam_preset.ccp");
            File.Copy(savedPath, fallbackPath, true); ScanLocalProfileDirectory();
            comboPresets.SelectedIndex = comboPresets.Items.IndexOf(Path.GetFileNameWithoutExtension(savedPath));
            MessageBox.Show("Profile created successfully and registered as system boot fallback lock!", "Success");
        }
    }

    private void btnSaveExisting_Click(object? sender, EventArgs e)
    {
        if (comboPresets.SelectedIndex == -1) { MessageBox.Show("Please select an existing profile from the list to overwrite.", "Notice"); return; }
        if (comboDevices.SelectedIndex == -1) return;
        var selectedDevice = webcams[comboDevices.SelectedIndex];
        var settings = WebcamController.GetCurrentSettings(selectedDevice.DevicePath);
        var preset = new CameraPreset { DeviceName = selectedDevice.Name, DevicePath = selectedDevice.DevicePath, Settings = settings };
        string profileName = comboPresets.SelectedItem.ToString()!; string path = trackedPresets[profileName];
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(preset, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webcam_preset.ccp");
        File.Copy(path, fallbackPath, true); MessageBox.Show($"Profile '{profileName}' successfully updated and committed to disk.", "Overwrite Successful");
    }

    private void comboPresets_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (comboPresets.SelectedIndex == -1) return;
        string profileName = comboPresets.SelectedItem.ToString()!; string path = trackedPresets[profileName];
        var preset = ConfigService.LoadPresetFromPath(path);
        if (preset != null) { WebcamController.ApplySettings(preset.DevicePath, preset.Settings); BuildDynamicSliders(); }
    }

    private void btnBrowseLoad_Click(object? sender, EventArgs e)
    {
        var preset = ConfigService.LoadPresetDialog();
        if (preset != null)
        {
            WebcamController.ApplySettings(preset.DevicePath, preset.Settings);
            RefreshHardwareTree(); BuildDynamicSliders(); ScanLocalProfileDirectory();
            MessageBox.Show("External profile loaded successfully.", "Loaded");
        }
    }

    private void btnAdvanced_Click(object? sender, EventArgs e)
    {
        if (comboDevices.SelectedIndex == -1 || webcams.Count == 0) return;
        var selectedDevice = webcams[comboDevices.SelectedIndex];
        WebcamController.ShowNativeConfigurationWindow(selectedDevice.DevicePath, this.Handle);
        BuildDynamicSliders();
    }

    private void ApplyFallbackStartupPreset()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webcam_preset.ccp");
        var preset = ConfigService.LoadPresetFromPath(path);
        if (preset != null) WebcamController.ApplySettings(preset.DevicePath, preset.Settings);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (this.WindowState == FormWindowState.Minimized) { this.Hide(); this.ShowInTaskbar = false; notifyIcon1.Visible = true; }
    }

    private void chkRunAtLogin_CheckedChanged(object? sender, EventArgs e)
    {
        using RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)!;
        if (chkRunAtLogin.Checked) rk.SetValue("WebcamPresetSaver", $"\"{Application.ExecutablePath}\" --silent");
        else rk.DeleteValue("WebcamPresetSaver", false);
    }

    private void CheckRegistryStartup()
    {
        using RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false)!;
        if (rk.GetValue("WebcamPresetSaver") != null) chkRunAtLogin.Checked = true;
    }
}
