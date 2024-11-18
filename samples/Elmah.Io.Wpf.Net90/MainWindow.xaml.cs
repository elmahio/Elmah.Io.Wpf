using Elmah.Io.Client;
using System.Windows;

namespace Elmah.Io.Wpf.Net90
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void AddBreadcrumbBtn_Click(object sender, RoutedEventArgs e)
        {
            ElmahIoWpf.AddBreadcrumb(new Breadcrumb(DateTime.UtcNow, "Warning", "Warning", "I tell you, ever since he got that Master Control Program..."));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S112:General or reserved exceptions should never be thrown", Justification = "For test purposes only")]
        private void ThrowExceptionBtn_Click(object sender, RoutedEventArgs e)
        {
            throw new ApplicationException("The system's got more bugs than a bait store");
        }
    }
}