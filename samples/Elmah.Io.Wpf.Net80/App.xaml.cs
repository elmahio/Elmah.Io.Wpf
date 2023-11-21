using System.Windows;

namespace Elmah.Io.Wpf.Net80
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
                Application = "WPF on .NET 8",

                // Use the OnFilter action to ignore specific log messages
                //OnFilter = msg =>
                //{
                //    return msg.Type.Equals("System.NullReferenceException");
                //},

                // Use the OnMessage action to decorate all log messages
                //OnMessage = msg =>
                //{
                //    msg.Version = "8.0.0";
                //},
            });
        }
    }

}
