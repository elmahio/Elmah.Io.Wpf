﻿using Elmah.Io.Client;
using System;

namespace Elmah.Io.Wpf
{
    /// <summary>
    /// Options for setting up elmah.io logging from WPF.
    /// </summary>
    public class ElmahIoWpfOptions
    {
        /// <summary>
        /// The API key from the elmah.io UI.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// The id of the log to send messages to.
        /// </summary>
        public Guid LogId { get; set; }

        /// <summary>
        /// An application name to put on all error messages.
        /// </summary>
        public string Application { get; set; }

        /// <summary>
        /// Register an action to be called before logging an error. Use the OnMessage action to
        /// decorate error messages with additional information.
        /// </summary>
        public Action<CreateMessage> OnMessage { get; set; }

        /// <summary>
        /// Register an action to be called if communicating with the elmah.io API fails.
        /// You can use this callback to log the error through which ever logging framework
        /// you may use.
        /// </summary>
        public Action<CreateMessage, Exception> OnError { get; set; }

        /// <summary>
        /// Register an action to filter log messages. Use this to add client-side ignore
        /// of some error messages. If the filter action returns true, the error is ignored.
        /// </summary>
        public Func<CreateMessage, bool> OnFilter { get; set; }

        /// <summary>
        /// The maximum number of breadcrumbs to store in-memory. Default = 10.
        /// </summary>
        public int MaximumBreadcrumbs { get; set; } = 10;
    }
}
