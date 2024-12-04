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
using System.Windows.Navigation;

namespace Elmah.Io.Wpf
{
    /// <summary>
    /// Main class used to interact with the elmah.io API from WPF.
    /// </summary>
    public static class ElmahIoWpf
    {
        private static readonly string _assemblyVersion = typeof(ElmahIoWpf).Assembly.GetName().Version.ToString();
        private static readonly string _presentationFrameworkAssemblyVersion = typeof(Application).Assembly.GetName().Version.ToString();

        private static ElmahIoWpfOptions _options;
        private static IElmahioAPI _logger;
        private static List<Breadcrumb> _breadcrumbs;

        /// <summary>
        /// Initialize logging of all uncaught errors to elmah.io.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Argument", "S3928")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Argument", "CA2208")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Argument", "IDE0079")]
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
                Url = Url(),
                ServerVariables =
                [
                    new("User-Agent", $"X-ELMAHIO-APPLICATION; OS=Windows; OSVERSION={Environment.OSVersion.Version}; ENGINE=WPF"),
                ]
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
            string message = null;
            if (e.Source is Button button)
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

        private static string Url()
        {
            var application = Application.Current;
            if (application.Windows == null || application.Windows.Count < 1) return null;

            var activeWindow = application
                .Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive);

            if (activeWindow == null) return null;

            try
            {
                var uri = BaseUriHelper.GetBaseUri(activeWindow);
                return uri?.AbsolutePath;
            }
            catch
            {
                return null;
            }
        }

        private static string Hostname()
        {
            var machineName = Environment.MachineName;
            if (!string.IsNullOrWhiteSpace(machineName)) return machineName;

            return Environment.GetEnvironmentVariable("COMPUTERNAME");
        }

        private static IList<Breadcrumb> Breadcrumbs()
        {
            if (_breadcrumbs == null || _breadcrumbs.Count == 0) return [];

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
            var application = Application.Current;
            var properties = application.Properties;
            foreach (var key in properties.Keys)
            {
                var value = properties[key];
                if (value != null) items.Add(new Item(key.ToString(), value.ToString()));
            }

            if (exception != null)
            {
                items.AddRange(exception.ToDataList());

                // The Dispatcher is using the Data dictionary on exceptions for some internal bookkeeping. This results in an empty
                // item being added with the key System.Object and a null value. Since this will never be interesting for anyone
                // outside of the Dispatcher we remove it. The source code doing this small trick is here:
                // https://github.com/dotnet/wpf/blob/ed058c1ab3f5594110731354794c5dfa0debdbd4/src/Microsoft.DotNet.Wpf/src/WindowsBase/System/Windows/Threading/Dispatcher.cs#L2755-L2767
                var exceptionDataKey = items.Find(i => i.Key.EndsWith(".System.Object") && string.IsNullOrWhiteSpace(i.Value));
                if (exceptionDataKey != null) items.Remove(exceptionDataKey);
            }

            if (application.MainWindow?.Width > 0) items.Add(new Item("Browser-Width", ((int)application.MainWindow.Width).ToString()));
            if (application.MainWindow?.Height > 0) items.Add(new Item("Browser-Height", ((int)application.MainWindow.Height).ToString()));
            if (SystemParameters.PrimaryScreenWidth > 0) items.Add(new Item("Screen-Width", ((int)SystemParameters.PrimaryScreenWidth).ToString()));
            if (SystemParameters.PrimaryScreenWidth > 0) items.Add(new Item("Screen-Height", ((int)SystemParameters.PrimaryScreenHeight).ToString()));

            AddMachine(items);

            return items;
        }

        // Credit goes to: https://weblog.west-wind.com/posts/2023/Feb/02/Basic-Windows-Machine-Hardware-information-from-WMI-for-Exception-Logging-from-NET
        private static void AddMachine(List<Item> items)
        {
            try
            {
                var machineBuilder = new StringBuilder();

                using (var searcher = new System.Management.ManagementObjectSearcher("Select Manufacturer, Model from Win32_ComputerSystem"))
                {
                    using var managementObjects = searcher.Get();
                    foreach (var item in managementObjects)
                    {
                        string manufacturer = item["Manufacturer"].ToString();
                        string model = item["Model"].ToString();
                        machineBuilder.Append(manufacturer).Append(' ').Append(model);
                    }
                }

                using (var searcher = new System.Management.ManagementObjectSearcher(
                       "Select * from Win32_DisplayConfiguration"))
                {
                    using var managementObjects = searcher.Get();
                    foreach (var item in managementObjects)
                    {
                        string gpu = item["Description"].ToString();

                        machineBuilder.Append(", ").Append(gpu);
                    }
                }

                var machine = machineBuilder.ToString();
                if (!string.IsNullOrWhiteSpace(machine))
                {
                    items.Add(new Item("Machine", machine));
                }
            }
            catch
            {
                // In case an error happened while trying to get the machine property, don't log anything about the machine.
                // The entire logging request should not fail in case there's a problem getting machine details.
            }
        }

        private static string UserAgent()
        {
            return new StringBuilder()
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Wpf", _assemblyVersion)).ToString())
                .Append(' ')
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("PresentationFramework", _presentationFrameworkAssemblyVersion)).ToString())
                .ToString();
        }
    }
}
