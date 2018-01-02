using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Monitor.Shared.Logging;
using System.Fabric.Description;
using Monitor.Shared.Logging.AppInsights;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

namespace Monitor.EventLogSender
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class EventLogSenderService : StatelessService
    {
        /// <summary>
        /// Logger for performance counter data
        /// </summary>
        private ILogger _logger = null;

        /// <summary>
        /// Lock for managed resources
        /// </summary>
        private Object thisLock = new Object();

        private const string AppInsightsConfigSectionName = "AppInsights";
        private const string AppInsightsTelemetryKeyConfigurationKey = "TelemetryKey";

        /// <summary>
        /// Event log listener
        /// </summary>
        private EventLogListener _eventLogListener = null;

        public EventLogSenderService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Called when the service is started
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override Task OnOpenAsync(CancellationToken cancellationToken)
        {
            // Get the configuration settings and monitor for changes.
            this.Context.CodePackageActivationContext.ConfigurationPackageModifiedEvent += this.CodePackageActivationContext_ConfigurationPackageModifiedEvent;

            ConfigurationPackage configPackage = this.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            if (null != configPackage)
            {
                lock (thisLock)
                {
                    this._logger = InitializeLogger(configPackage.Settings);
                }
            }

            return base.OnOpenAsync(cancellationToken);
        }

        protected override Task OnCloseAsync(CancellationToken cancellationToken)
        {
            if (_logger != null)
                this._logger.Dispose();
            if(_eventLogListener != null)
                this._eventLogListener.Dispose();

            return base.OnCloseAsync(cancellationToken);
        }

        /// <summary>
        /// Called when a configuration package is modified.
        /// </summary>
        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            if ("Config" == e.NewPackage.Description.Name)
            {
                lock (thisLock)
                {
                    this._logger.Dispose();
                    this._logger = InitializeLogger(e.NewPackage.Settings);
                }
            }
        }

        private ILogger InitializeLogger(ConfigurationSettings settings)
        {
            Debug.Assert(settings.Sections.Contains(AppInsightsConfigSectionName));
            var appInsightsConfigSection = settings.Sections[AppInsightsConfigSectionName];

            Debug.Assert(appInsightsConfigSection.Parameters.Contains(AppInsightsTelemetryKeyConfigurationKey));
            var telemetryKey = appInsightsConfigSection.Parameters[AppInsightsTelemetryKeyConfigurationKey];

            //TODO: Encryption
            return new AppInsightsUnsampledLogger(telemetryKey.Value);
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            _eventLogListener = new EventLogListener();
            _eventLogListener.LogWritten += _eventLogListener_LogWritten;

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        /// <summary>
        /// Refreshes the FabricClient instance.
        /// </summary>
        private void RefreshEventLogListener()
        {
            EventLogListener old = Interlocked.CompareExchange<EventLogListener>(ref _eventLogListener, new EventLogListener(), _eventLogListener);
            old?.Dispose();
            _eventLogListener.LogWritten += _eventLogListener_LogWritten;
        }

        private void _eventLogListener_LogWritten(RetrievedLog log)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("Source", log.Source);
            _logger.ReportTraceAsync(log.Description, TraceSeverity.Error, dict);
        }
    }
}
