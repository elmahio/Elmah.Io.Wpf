using System;
using System.Windows;

namespace Elmah.Io.Wpf.NetFramework48
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
                Application = "WPF on .NET Framework 4.8",
            });
        }
    }
}
