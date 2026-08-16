using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using NOVORA.Launcher.Models;
using NOVORA.Launcher.Services;
using NOVORA.Launcher.ViewModels;

namespace NOVORA.Launcher;

public partial class MainWindow : Window
{
    private readonly NovoraPaths _paths = new();
    private readonly AdbService _adb;
    private readonly MonitorService _monitorService = new();
    private readonly MainViewModel _viewModel = new();
    private readonly GnirehtetService _gnirehtet;
    private readonly SettingsService _settingsService = new();
    private readonly OutputProfileService _outputProfileService = new();
    private readonly UpdateService _updateService = new();

    private IReadOnlyList<MonitorInfo> _monitors = Array.Empty<MonitorInfo>();
    private int _connectedDeviceCount;
    private Process? _scrcpyProcess;

    private bool _stopping;
    private bool _closeCleanupStarted;
    private bool _allowClose;
    private bool _refreshingDevices;
    private bool _checkingUpdate;

    public MainWindow()
    {
        InitializeComponent();
        _adb = new AdbService(_paths);
        _gnirehtet = new GnirehtetService(_paths);
        DataContext = _viewModel;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _paths.ValidateRequiredTools();
            LoadSettings();
            LoadMonitors();
            await RefreshDeviceAsync();
        }
        catch (Exception ex)
        {
            _viewModel.ConnectionStatus = "Error de inicialización";
            MessageBox.Show(ex.Message, "NOVORA", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();
        _viewModel.AudioEnabled = settings.AudioEnabled;
    }

    private void LoadMonitors()
    {
        _monitors = _monitorService.GetMonitors();
        _viewModel.Monitors = _monitors;
        var saved = _settingsService.Load().SelectedMonitorLabel;
        _viewModel.SelectedMonitor = _monitors.FirstOrDefault(m => string.Equals(m.DisplayLabel, saved, StringComparison.OrdinalIgnoreCase))
            ?? _monitorService.GetBestMonitor(_monitors);
        RefreshOutputProfile();
    }

    private async Task RefreshDeviceAsync()
    {
        var devices = await _adb.GetDevicesAsync();
        _connectedDeviceCount = devices.Count;
        _viewModel.Devices = devices;
        var settings = _settingsService.Load();
        var selected = devices.FirstOrDefault(d => !string.IsNullOrWhiteSpace(settings.SelectedDeviceSerial) && string.Equals(d.Serial, settings.SelectedDeviceSerial, StringComparison.OrdinalIgnoreCase));
        _viewModel.Device = selected ?? (devices.Count == 1 ? devices[0] : new DeviceInfo());

        if (_viewModel.Device.Connected)
        {
            _viewModel.ConnectionStatus = $"● CONECTADO • {_viewModel.Device.Model}";
            var mode = _viewModel.Device.BestDisplayMode;
            _viewModel.PerformanceSummary = mode is null ? "Pantalla detectada; capacidades pendientes." : $"Hasta {mode.Width}x{mode.Height} @ {mode.RefreshRateHz:0.#} Hz";
            RefreshOutputProfile();
        }
        else
        {
            _viewModel.ConnectionStatus = devices.Count == 0 ? "Sin dispositivo" : "Selecciona un dispositivo";
            _viewModel.PerformanceSummary = devices.Count == 0 ? "Conecta el teléfono por USB y autoriza la depuración ADB." : $"{devices.Count} dispositivos detectados. Selecciona el que quieras utilizar.";
            _viewModel.OutputProfile = null;
        }
    }

    private async void RefreshDevices_Click(object sender, RoutedEventArgs e)
    {
        if (_refreshingDevices) return;
        try
        {
            _refreshingDevices = true;
            if (RefreshDevicesButton is not null) RefreshDevicesButton.IsEnabled = false;
            _viewModel.ConnectionStatus = "Buscando dispositivos...";
            await RefreshDeviceAsync();
        }
        catch (Exception ex)
        {
            _viewModel.ConnectionStatus = "Error al actualizar";
            MessageBox.Show($"No fue posible actualizar los dispositivos Android:\n\n{ex.Message}", "NOVORA — Actualizar dispositivos", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _refreshingDevices = false;
            if (RefreshDevicesButton is not null) RefreshDevicesButton.IsEnabled = true;
        }
    }

    private void Device_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_viewModel.Device is null || !_viewModel.Device.Connected) return;
        try
        {
            var settings = _settingsService.Load();
            settings.SelectedDeviceSerial = _viewModel.Device.Serial;
            _settingsService.Save(settings);
            _viewModel.ConnectionStatus = $"● CONECTADO • {_viewModel.Device.Model}";
            var mode = _viewModel.Device.BestDisplayMode;
            _viewModel.PerformanceSummary = mode is null ? "Pantalla detectada; capacidades pendientes." : $"Hasta {mode.Width}x{mode.Height} @ {mode.RefreshRateHz:0.#} Hz";
            RefreshOutputProfile();
        }
        catch { }
    }

    private void Monitor_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => RefreshOutputProfile();

    private void RefreshOutputProfile()
    {
        if (!_viewModel.Device.Connected || _viewModel.SelectedMonitor is null) return;
        try
        {
            var profile = _outputProfileService.Calculate(_viewModel.Device, _viewModel.SelectedMonitor);
            _viewModel.OutputProfile = profile;
            _viewModel.PerformanceSummary = profile.Summary;
        }
        catch { _viewModel.OutputProfile = null; }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) { Maximize_Click(sender, e); return; }
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowClose || _closeCleanupStarted) return;
        e.Cancel = true;
        _closeCleanupStarted = true;
        try { await StopTransmissionAsync(); }
        finally { _allowClose = true; Close(); }
    }

    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _viewModel.ConnectionStatus = "● DETENIENDO NOVORA";
            await StopTransmissionAsync();
            _viewModel.ConnectionStatus = "NOVORA detenido";
        }
        catch (Exception ex)
        {
            _viewModel.ConnectionStatus = "Detención completada con avisos";
            MessageBox.Show($"NOVORA detuvo la transmisión, pero se produjo un aviso durante la limpieza:\n\n{ex.Message}", "NOVORA — Detener", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task StopTransmissionAsync()
    {
        if (_stopping) return;
        _stopping = true;
        var serial = _viewModel.Device.Connected ? _viewModel.Device.Serial : _gnirehtet.ActiveSerial;
        var scrcpy = _scrcpyProcess;
        try
        {
            if (scrcpy is { HasExited: false })
            {
                try { scrcpy.Exited -= ScrcpyProcess_Exited; } catch { }
                try { scrcpy.CloseMainWindow(); } catch { }
                try { if (!scrcpy.WaitForExit(1200)) scrcpy.Kill(entireProcessTree: true); } catch { }
            }
            try { scrcpy?.Dispose(); } catch { }
            _scrcpyProcess = null;
            using var cleanupCts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            await _gnirehtet.StopAsync(serial, cleanupCts.Token);
            await _adb.StopServerIfNoOtherDevicesAsync(serial, cleanupCts.Token);
            _viewModel.GnirehtetStatus = "No iniciado";
        }
        catch (OperationCanceledException) { }
        finally { _stopping = false; }
    }

    private void Configuration_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new SettingsWindow(_viewModel) { Owner = this };
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "NOVORA — Configuración", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.Device.Connected || string.IsNullOrWhiteSpace(_viewModel.Device.Serial))
        {
            MessageBox.Show("Conecta y autoriza un dispositivo Android por USB.", "NOVORA", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_scrcpyProcess is { HasExited: false })
        {
            MessageBox.Show("La transmisión ya está activa.", "NOVORA", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var monitor = _viewModel.SelectedMonitor ?? _monitorService.GetBestMonitor(_monitors);
        try
        {
            _viewModel.ConnectionStatus = "● PREPARANDO NOVORA";
            var profile = _outputProfileService.Calculate(_viewModel.Device, monitor);
            _viewModel.OutputProfile = profile;
            _viewModel.PerformanceSummary = profile.Summary;
            var scrcpy = new ScrcpyService(_paths);
            _scrcpyProcess = scrcpy.StartOptimized(_viewModel.Device, monitor, profile, _viewModel.AudioEnabled);
            _scrcpyProcess.EnableRaisingEvents = true;
            _scrcpyProcess.Exited += ScrcpyProcess_Exited;
            _viewModel.ConnectionStatus = "● NOVORA ACTIVO";
        }
        catch (Exception ex)
        {
            _viewModel.ConnectionStatus = "Error al iniciar";
            MessageBox.Show(ex.Message, "NOVORA", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ScrcpyProcess_Exited(object? sender, EventArgs e)
    {
        if (_stopping) return;
        Dispatcher.BeginInvoke(new Action(async () =>
        {
            try
            {
                _scrcpyProcess?.Dispose();
                _scrcpyProcess = null;
                var serial = _viewModel.Device.Connected ? _viewModel.Device.Serial : _gnirehtet.ActiveSerial;
                using var cleanupCts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                await _gnirehtet.StopAsync(serial, cleanupCts.Token);
                await _adb.StopServerIfNoOtherDevicesAsync(serial, cleanupCts.Token);
                _viewModel.GnirehtetStatus = "No iniciado";
                _viewModel.ConnectionStatus = "Transmisión finalizada";
            }
            catch (OperationCanceledException) { _viewModel.ConnectionStatus = "Transmisión finalizada"; }
            catch { _viewModel.ConnectionStatus = "Transmisión finalizada"; }
        }));
    }

    private async void Gnirehtet_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.Device.Connected)
        {
            MessageBox.Show("Conecta y autoriza un dispositivo Android por USB.", "Gnirehtet", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            if (_gnirehtet.IsActive)
            {
                _viewModel.GnirehtetStatus = "Deteniendo...";
                await _gnirehtet.StopAsync(_viewModel.Device.Serial);
                _viewModel.GnirehtetStatus = "No iniciado";
                await _adb.StopServerIfNoOtherDevicesAsync(_viewModel.Device.Serial);
                return;
            }
            _viewModel.GnirehtetStatus = "Iniciando...";
            var result = await _gnirehtet.StartAsync(_viewModel.Device, _connectedDeviceCount);
            _viewModel.GnirehtetStatus = result.Success ? "Activo" : $"Error: {result.Message}";
            if (!result.Success) MessageBox.Show(result.Message, "NOVORA — Gnirehtet", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            _viewModel.GnirehtetStatus = "Error";
            MessageBox.Show(ex.Message, "NOVORA — Gnirehtet", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        if (_checkingUpdate) return;
        _checkingUpdate = true;
        try
        {
            _viewModel.ConnectionStatus = "Buscando actualización...";
            var update = await _updateService.CheckForUpdateAsync();
            if (update is null)
            {
                _viewModel.ConnectionStatus = _viewModel.Device.Connected ? $"● CONECTADO • {_viewModel.Device.Model}" : "NOVORA actualizado";
                MessageBox.Show($"Ya tienes la versión {_updateService.CurrentVersion} de NOVORA.\n\nNo hay una versión estable más reciente disponible.", "NOVORA — Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var result = MessageBox.Show($"Hay una nueva versión de NOVORA disponible.\n\nVersión actual: {_updateService.CurrentVersion}\nNueva versión: {update.Version}\n\n¿Quieres descargar e instalar la actualización ahora?", "NOVORA — Actualización disponible", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result != MessageBoxResult.Yes) return;
            _viewModel.ConnectionStatus = $"Descargando NOVORA {update.Version}...";
            var progress = new Progress<int>(value => _viewModel.ConnectionStatus = $"Descargando actualización... {value}%");
            var installerPath = await _updateService.DownloadInstallerAsync(update, progress);
            var launch = new ProcessStartInfo { FileName = installerPath, UseShellExecute = true, Verb = "runas" };
            Process.Start(launch);
            _allowClose = true;
            Close();
        }
        catch (OperationCanceledException) { _viewModel.ConnectionStatus = "Actualización cancelada"; }
        catch (Exception ex)
        {
            _viewModel.ConnectionStatus = "No se pudo comprobar la actualización";
            MessageBox.Show($"No fue posible comprobar o instalar la actualización.\n\n{ex.Message}", "NOVORA — Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { _checkingUpdate = false; }
    }

    protected override void OnClosed(EventArgs e)
    {
        try { if (_scrcpyProcess is { HasExited: false }) _scrcpyProcess.Kill(entireProcessTree: true); } catch { }
        try { _scrcpyProcess?.Dispose(); } catch { }
        _scrcpyProcess = null;
        _gnirehtet.Dispose();
        base.OnClosed(e);
    }
}
