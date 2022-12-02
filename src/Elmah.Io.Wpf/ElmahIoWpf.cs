using Elmah.Io.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Elmah.Io.Wpf
{
    /// <summary>
    /// Main class used to interact with the elmah.io API from WPF.
    /// </summary>
    public static class ElmahIoWpf
    {
        internal static string _assemblyVersion = typeof(ElmahIoWpf).Assembly.GetName().Version.ToString();
        internal static string _presentationFrameworkAssemblyVersion = typeof(Application).Assembly.GetName().Version.ToString();

        private static ElmahIoWpfOptions _options;
        private static IElmahioAPI _logger;
        private static List<Breadcrumb> _breadcrumbs;

        /// <summary>
        /// Initialize logging of all uncaught errors to elmah.io.
        /// </summary>
        public static void Init(ElmahIoWpfOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(options.ApiKey)) throw new ArgumentNullException(nameof(options.ApiKey));
            if (options.LogId == Guid.Empty) throw new ArgumentException(nameof(options.LogId));

            _options = options;
            _breadcrumbs = new List<Breadcrumb>(1 + options.MaximumBreadcrumbs);
            _logger = ElmahioAPI.Create(options.ApiKey, new ElmahIoOptions
            {
                Timeout = new TimeSpan(0, 0, 5),
                UserAgent = UserAgent(),
            });

            _logger.Messages.OnMessageFail += (sender, args) =>
            {
                options.OnError?.Invoke(args.Message, args.Error);
            };

            EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(Button_Click));
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(Window_Loaded));
            EventManager.RegisterClassHandler(typeof(Window), Window.UnloadedEvent, new RoutedEventHandler(Window_Unloaded));

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                Log(args.ExceptionObject as Exception);

            TaskScheduler.UnobservedTaskException += (sender, args) =>
                Log(args.Exception);

            Application.Current.Dispatcher.UnhandledException += (sender, args) =>
            {
                if (!Debugger.IsAttached)
                    Log(args.Exception);
            };
        }

        /// <summary>
        /// Log an exception to elmah.io manually.
        /// </summary>
        public static void Log(Exception exception)
        {
            var baseException = exception?.GetBaseException();
            var createMessage = new CreateMessage
            {
                DateTime = DateTime.UtcNow,
                Detail = exception?.ToString(),
                Type = baseException?.GetType().FullName,
                Title = baseException?.Message ?? "An error occurred",
                Data = PropertiesToData(exception),
                Severity = "Error",
                Source = baseException?.Source,
                User = WindowsIdentity.GetCurrent().Name,
                Hostname = Hostname(),
                Breadcrumbs = Breadcrumbs(),
                Application = _options.Application,
            };

            if (_options.OnFilter != null && _options.OnFilter(createMessage))
            {
                return;
            }

            _options.OnMessage?.Invoke(createMessage);

            try
            {
                _logger.Messages.Create(_options.LogId.ToString(), createMessage);
            }
            catch (Exception ex)
            {
                _options.OnError?.Invoke(createMessage, ex);
            }
        }

        /// <summary>
        /// Add a breadcrumb in-memory. Breadcrumbs will be added to errors when logged
        /// either automatically or manually.
        /// </summary>
        public static void AddBreadcrumb(Breadcrumb breadcrumb)
        {
            _breadcrumbs.Add(breadcrumb);

            if (_breadcrumbs.Count >= _options.MaximumBreadcrumbs)
            {
                var oldest = _breadcrumbs.OrderBy(b => b.DateTime).First();
                _breadcrumbs.Remove(oldest);
            }
        }

        private static void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = e.Source as Button;
            string message = null;
            if (button != null)
            {
                if (!string.IsNullOrWhiteSpace(button.Name)) message = button.Name;
                else if (button.Content is string) message = button.Content.ToString();
            }
            var breadcrumb = new Breadcrumb(DateTime.UtcNow, "Information", "Click", message);
            AddBreadcrumb(breadcrumb);

        }

        private static void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            AddWindowEvent(e.Source as Window, "Unloaded");
        }

        private static void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AddWindowEvent(e.Source as Window, "Loaded");
        }

        private static void AddWindowEvent(Window window, string action)
        {
            string target = "window";
            if (window != null)
            {
                if (!string.IsNullOrWhiteSpace(window.Name)) target = window.Name;
                else if (!string.IsNullOrWhiteSpace(window.Title)) target = window.Title;
            }

            var breadcrumb = new Breadcrumb(DateTime.UtcNow, "Information", "Navigation", $"{action} {target}");
            AddBreadcrumb(breadcrumb);
        }

        private static string Hostname()
        {
            var machineName = Environment.MachineName;
            if (!string.IsNullOrWhiteSpace(machineName)) return machineName;

            return Environment.GetEnvironmentVariable("COMPUTERNAME");
        }

        private static IList<Breadcrumb> Breadcrumbs()
        {
            if (_breadcrumbs == null || _breadcrumbs.Count == 0) return null;

            var utcNow = DateTime.UtcNow;

            // Set default values on properties not set
            foreach (var breadcrumb in _breadcrumbs)
            {
                if (!breadcrumb.DateTime.HasValue) breadcrumb.DateTime = utcNow;
                if (string.IsNullOrWhiteSpace(breadcrumb.Severity)) breadcrumb.Severity = "Information";
                if (string.IsNullOrWhiteSpace(breadcrumb.Action)) breadcrumb.Action = "Log";
            }

            var breadcrumbs = _breadcrumbs.OrderByDescending(l => l.DateTime).ToList();
            _breadcrumbs.Clear();
            return breadcrumbs;
        }

        private static List<Item> PropertiesToData(Exception exception)
        {
            var items = new List<Item>();
            var properties = Application.Current.Properties;
            foreach (var key in properties.Keys)
            {
                var value = properties[key];
                if (value != null) items.Add(new Item(key.ToString(), value.ToString()));
            }

            if (exception != null)
            {
                items.AddRange(exception.ToDataList());
            }

            return items;
        }

        private static string UserAgent()
        {
            return new StringBuilder()
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Wpf", _assemblyVersion)).ToString())
                .Append(" ")
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("PresentationFramework", _presentationFrameworkAssemblyVersion)).ToString())
                .ToString();
        }
    }
}
