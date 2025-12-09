#pragma warning disable S125 // Sections of code should not be commented out
using System.Windows;

namespace Elmah.Io.Wpf.Sample.Net10
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
                Application = "WPF on .NET 10",

                // Use the OnFilter action to ignore specific log messages
                //OnFilter = msg =>
                //{
                //    return msg.Type.Equals("System.NullReferenceException");
                //},

                // Use the OnMessage action to decorate all log messages
                //OnMessage = msg =>
                //{
                //    msg.Version = "10.0.0";
                //},
            });
        }
    }
}
#pragma warning restore S125 // Sections of code should not be commented out