using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WPF_Test_PLC20260124; // Namespace of MainViewModel

namespace GantrySCADA
{
    public partial class App : Application
    {
        public App()
        {
            var services = new ServiceCollection();
            services.AddWpfBlazorWebView();
#if DEBUG
            services.AddBlazorWebViewDeveloperTools();
#endif

            // Register ViewModel as Singleton
            services.AddSingleton<MainViewModel>();

            Resources.Add("services", services.BuildServiceProvider());
        }
    }
}
