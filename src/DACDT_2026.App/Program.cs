using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace DACDT_2026
{
    internal static class Program
    {
        private static readonly string LogPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");

        private static void LogException(string source, object exObj)
        {
            try
            {
                string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string msg = $"[{ts}] {source}\n{exObj}\n{new string('-', 60)}\n";
                File.AppendAllText(LogPath, msg);
                System.Diagnostics.Debug.WriteLine(msg);
            }
            catch { }
        }

        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                LogException("AppDomain UnhandledException (terminating=" + e.IsTerminating + ")", e.ExceptionObject);
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                LogException("UnobservedTaskException", e.Exception);
                e.SetObserved();
            };

            try
            {
                LogException("App Start", "Application started");
                var app = new Application();
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("Views/Styles.xaml", UriKind.Relative)
                });
                app.Resources.Add("BoolToVisibilityConverter", new BoolToVisibilityConverter());
                app.Resources.Add("InverseBoolToVisibilityConverter", new InverseBoolToVisibilityConverter());
                app.Resources.Add("BoolToStatusBrushConverter", new BoolToStatusBrushConverter());
                app.DispatcherUnhandledException += (sender, e) =>
                {
                    LogException("WPF DispatcherUnhandledException", e.Exception);
                    e.Handled = true;
                };
                app.Run(new Form1());
                LogException("App End", "Application exited normally");
            }
            catch (Exception ex)
            {
                LogException("Main Exception", ex);
                throw;
            }
        }
    }
}
