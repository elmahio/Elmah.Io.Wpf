using System.Windows;

namespace Elmah.Io.Wpf.Net80
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "For test purposes only")]
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
