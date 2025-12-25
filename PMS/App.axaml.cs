using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PMS.Core.Services;
using PMS.ViewModels;
using PMS.Views.Window;
using Serilog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PMS.Core.Extensions;
using PMS.Core.Models.Common;
using PMS.Core.Settings;
using PMS.Core.Services.Interfaces;

namespace PMS
{
    public class App : Application
    {
        // just for debug purposes on windows
#if DEBUG
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private static void InitializeDebugConsole()
        {
            AllocConsole();
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }
#else
        public static void InitDebugW() { } 
#endif

        private IServiceProvider _serviceProvider = null!;
        private IServiceCollection _serviceCollection = null!;

        private IMessageBoxService _messageBoxService = null!;
        private ILocalizationService _localizationService = null!;

        public static ApplicationSettings ApplicationSettings { get; set; }
        public static LoggingSettings LoggingSettings { get; set; }

        private static Mutex? Mutex { get; set; }
        private readonly string _mutexName = 
            SApplicationSettings.ApplicationName + 
            SApplicationSettings.Version + 
            Environment.MachineName;


        public override void Initialize()
        {
#if DEBUG
            InitializeDebugConsole();
#endif
            ConfigureLogging();

            AvaloniaXamlLoader.Load(this);

            _serviceCollection = new ServiceCollection();
            ConfigureServices(_serviceCollection);
            _serviceProvider = _serviceCollection.BuildServiceProvider();

            var appSettingsService = GetService<IApplicationSettingsService>();
            var logSettingsService = GetService<ILoggingSettingsService>();

            ApplicationSettings = appSettingsService.LoadSettings();
            LoggingSettings = logSettingsService.LoadSettings();
            

            var culture = new CultureInfo("uk-UA");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

        }
        
        public override async void OnFrameworkInitializationCompleted()
        {
            try
            {
                Mutex = new Mutex(true, _mutexName, out bool createdNew);
                if (!createdNew)
                {
                    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
                    {
                        var warningWindow = new IrWindow
                        {
                            Topmost = true
                        };

                        warningWindow.Closed += (_, _) =>
                        {
                            Mutex?.Dispose();
                            Mutex = null;
                            Exit(1);
                        };

                        warningWindow.Show();
                        return;
                    }
                }
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                Mutex = new Mutex(true, _mutexName, out _);
            }
            catch (AbandonedMutexException)
            {
                Mutex = new Mutex(true, _mutexName, out _);
            }

            _messageBoxService = GetService<IMessageBoxService>();
            _localizationService = GetService<ILocalizationService>();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                try
                {
                    await StartApplicationFlowAsync();
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "Fatal error during application startup.");
                    Debug.WriteLine($"Error: {ex.Message}");
                    await _messageBoxService.ShowErrorAsync(
                        "Критична помилка при запуску програми.\nПрограма буде закрита.",
                        "Фатальна помилка",
                        ex);
                    Exit(1);
                }
            }
            base.OnFrameworkInitializationCompleted();
        }

        private async Task StartApplicationFlowAsync()
        {
            var dbSettingsService = GetService<IDatabaseSettingsService>();

            if (dbSettingsService.Exists())
            {
                var settings = dbSettingsService.LoadSettings();
                var configValid = DatabaseSettingsService.ValidateDatabaseSettings(out _, settings);
                var connectionTestSuccessful = await dbSettingsService.TestConnectionAsync(settings, settings.Username, settings.Password);

                while (!configValid || !connectionTestSuccessful)
                {
                    await SettingsViewModel.ShowSettingsWindowAsync();
                    configValid = DatabaseSettingsService.ValidateDatabaseSettings(out _, settings);
                    connectionTestSuccessful = await dbSettingsService.TestConnectionAsync(settings, settings.Username, settings.Password);
                }
            }
            else
            {
                await SettingsViewModel.ShowSettingsWindowAsync();
            }

            var autoLoginSuccess = await AuthViewModel.TryAutoLoginAsync();

            if (!autoLoginSuccess)
            {
                var loginSuccess = await AuthViewModel.ShowAuthWindowAsync();

                if (!loginSuccess)
                {
                    Exit(1);
                    return;
                }
            }

            await MainWindowViewModel.ShowMainWindow();
        }

        private static void ConfigureLogging()
        {

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("PMS/.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 5)
#if DEBUG
                .WriteTo.Console()
#endif
                .CreateLogger();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationServices();
        }

        public static T GetService<T>() where T : class
        {
            var app = Current as App;
            return app?._serviceProvider.GetRequiredService<T>()
                   ?? throw new InvalidOperationException("Service provider not initialized");
        }

        public static object GetService(Type serviceType)
        {
            var app = Current as App;
            return app?._serviceProvider.GetRequiredService(serviceType)
                   ?? throw new InvalidOperationException("Service provider not initialized");
        }

        public static void Exit(int code = 0)
        {
            if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown(code);
            }
            else
            {
                Environment.Exit(code);
            }
        }

        private static string? GetExecutablePath()
        {
            var executablePath = Environment.ProcessPath;

            if (string.IsNullOrEmpty(executablePath))
            {
                var currentProcess = Process.GetCurrentProcess();
                executablePath = currentProcess.MainModule?.FileName;
            }

            return executablePath;
        }

        public static async void Restart(int delayMs = 1000)
        {
            var executablePath = GetExecutablePath();

            if (string.IsNullOrEmpty(executablePath))
            {
                throw new InvalidOperationException("Unable to determine executable path");
            }

            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            var arguments = string.Join(" ", args.Select(arg =>
                arg.Contains(' ') ? $"\"{arg}\"" : arg));

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                UseShellExecute = false, 
                WorkingDirectory = Environment.CurrentDirectory
            };

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                if (executablePath.EndsWith(".dll"))
                {
                    startInfo.FileName = "dotnet";
                    startInfo.Arguments = $"\"{executablePath}\" {arguments}";
                }
                else
                {
                    startInfo.UseShellExecute = true;
                }
            }

            await Task.Run(async () =>
            {
                await Task.Delay(delayMs);
                Process.Start(startInfo);
            });
            Exit();
        }
    }


   
}