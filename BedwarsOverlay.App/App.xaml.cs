using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BedwarsOverlay.App.Services;

namespace BedwarsOverlay.App
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Handle UI thread exceptions
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Handle non-UI thread exceptions (background workers)
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Handle async Task exceptions
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(Window_Loaded));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                ClickThroughService.DisableMaximizing(window);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash(e.Exception, "UI Thread Unhandled Exception");
            e.Handled = true; // prevent immediate hard crash
            MessageBox.Show($"UI Thread Exception:\n{e.Exception.Message}\n\nStack trace logged to crash.log", 
                            "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogCrash(ex, "Domain Unhandled Exception");
                MessageBox.Show($"Background Thread Exception:\n{ex.Message}\n\nStack trace logged to crash.log", 
                                "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogCrash(e.Exception, "Unobserved Task Exception");
            e.SetObserved(); // prevent process crash
            MessageBox.Show($"Async Task Exception:\n{e.Exception.Message}\n\nStack trace logged to crash.log", 
                            "Task Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void LogCrash(Exception ex, string type)
        {
            try
            {
                string localAppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BedwarsOverlay");
                if (!Directory.Exists(localAppFolder))
                {
                    Directory.CreateDirectory(localAppFolder);
                }
                string logFile = Path.Combine(localAppFolder, "crash.log");
                
                string content = $"========================================\n" +
                                 $"Date: {DateTime.Now}\n" +
                                 $"Type: {type}\n" +
                                 $"Message: {ex.Message}\n" +
                                 $"Stack Trace:\n{ex.StackTrace}\n";
                if (ex.InnerException != null)
                {
                    content += $"Inner Exception: {ex.InnerException.Message}\n" +
                               $"Inner Stack Trace:\n{ex.InnerException.StackTrace}\n";
                }
                content += "========================================\n\n";

                File.AppendAllText(logFile, content);
            }
            catch
            {
                // Suppress logging issues to prevent infinite loops
            }
        }
    }
}
