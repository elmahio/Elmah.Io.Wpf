using Elmah.Io.Client;
using System;
using System.Windows;

namespace Elmah.Io.Wpf.NetFramework48
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

        private void ThrowExceptionBtn_Click(object sender, RoutedEventArgs e)
        {
            throw new ApplicationException("The system's got more bugs than a bait store");
        }
    }
}
