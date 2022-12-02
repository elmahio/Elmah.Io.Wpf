using System;
using System.Windows;

namespace Elmah.Io.Wpf.Net60
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            ElmahIoWpf.Init(new ElmahIoWpfOptions
            {
                ApiKey = "API_KEY",
                LogId = new Guid("LOG_ID"),
                Application = "WPF on .NET 6",
            });
        }
    }
}
