using Elmah.Io.Client;
using NSubstitute;
using NUnit.Framework;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Elmah.Io.Wpf.Test
{
    public class ElmahIoWpfTest
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void Test()
        {
            // Application needs to be initialized before running the test but we don't need the variable afterwards
            _ = new Application();

            var options = new ElmahIoWpfOptions
            {
                ApiKey = "hello",
                LogId = Guid.NewGuid(),
                Application = "MyApp",
            };
            ElmahIoWpf.Init(options);

            var messagesClient = Substitute.For<IMessagesClient>();
            var elmahIoClient = Substitute.For<IElmahioAPI>();
            elmahIoClient.Messages.Returns(messagesClient);

            var field = typeof(ElmahIoWpf).GetField("_logger", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, elmahIoClient);

            ElmahIoWpf.AddBreadcrumb(new Breadcrumb
            {
                DateTime = DateTime.UtcNow,
                Action = "Navigation",
                Message = "Opening app",
                Severity = "Information",
            });

            var window = new Window()
            {
                Name = "MyWindow",
                Width = 0,
                Height = 0,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false,
            };
            var btn = new Button
            {
                Name = "MyButton"
            };
            btn.Click += (sender, args) => { };
            window.Content = btn;
            window.Show();
            btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

            var ex = new ApplicationException("Oh no");
            ElmahIoWpf.Log(ex);

            messagesClient.Received().Create(Arg.Is<string>(s => s == options.LogId.ToString()), Arg.Is<CreateMessage>(msg => AssertMessage(msg, ex)));
        }

        private static bool AssertMessage(CreateMessage msg, ApplicationException ex)
        {
            if (msg.Title != "Oh no") return false;
            if (msg.Breadcrumbs.Count != 3) return false;
            var clickBreadcrumb = msg.Breadcrumbs.First();
            if (clickBreadcrumb.Action != "Click" || clickBreadcrumb.Severity != "Information" || clickBreadcrumb.Message != "MyButton") return false;
            var loadedBreadcrumb = msg.Breadcrumbs.Skip(1).First();
            if (loadedBreadcrumb.Action != "Navigation" || loadedBreadcrumb.Severity != "Information" || loadedBreadcrumb.Message != "Loaded MyWindow") return false;
            var openingBreadcrumb = msg.Breadcrumbs.Last();
            if (openingBreadcrumb.Action != "Navigation" || openingBreadcrumb.Severity != "Information" || openingBreadcrumb.Message != "Opening app") return false;

            if (string.IsNullOrWhiteSpace(msg.Detail)) return false;
            if (msg.Type != ex.GetType().FullName) return false;
            if (msg.Severity != "Error") return false;
            if (msg.Source != ex.Source) return false;
            if (msg.Application != "MyApp") return false;

            return true;
        }
    }
}
