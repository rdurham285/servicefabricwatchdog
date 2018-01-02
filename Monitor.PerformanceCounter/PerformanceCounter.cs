using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ApplicationInsights.Extensibility;
using System.Fabric.Description;
using System.Diagnostics;
using Monitor.Shared.Logging;
using Monitor.Shared.Logging.AppInsights;
using System.IO;
using Newtonsoft.Json;

namespace Monitor.PerformanceCounter
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class PerformanceCounter : StatelessService
    {
        //TODO: Make this configurable
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Configuration package instance.
        /// </summary>
        private ConfigurationSettings _settings = null;

        /// <summary>
        /// Performance counters being collected
        /// </summary>
        private IEnumerable<MonitorPerformanceCounter> _perfCounters = null;

        /// <summary>
        /// Logger for performance counter data
        /// </summary>
        private ILogger _logger = null;

        /// <summary>
        /// Lock for managed resources
        /// </summary>
        private Object thisLock = new Object();


        /// <summary>
        /// Expected keys for configuration files
        /// </summary>
        private const string PerfCounterConfigSectionName = "PerfCounters";
        private const string PerfCounterFilePathParameterName = "PerfCounterConfigFilePath";

        private const string AppInsightsConfigSectionName = "AppInsights";
        private const string AppInsightsTelemetryKeyConfigurationKey = "TelemetryKey";

        public PerformanceCounter(StatelessServiceContext context)
            : base(context)
        {
        }

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
                    this._settings = configPackage.Settings;
                    this._logger = InitializeLogger(this._settings);

                    if (this._perfCounters != null)
                    {
                        foreach (var counter in this._perfCounters)
                            counter.Dispose();
                    }
                    this._perfCounters = InitializePerformanceCounters(this._settings);
                }
            }

            return base.OnOpenAsync(cancellationToken);
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
                    this._settings = e.NewPackage.Settings;
                    this._logger.Dispose();
                    this._logger = InitializeLogger(this._settings);

                    if (this._perfCounters != null)
                    {
                        foreach (var counter in this._perfCounters)
                            counter.Dispose();
                    }
                    this._perfCounters = InitializePerformanceCounters(this._settings);
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

        private IEnumerable<MonitorPerformanceCounter> InitializePerformanceCounters(ConfigurationSettings settings)
        {
            Debug.Assert(settings.Sections.Contains(PerfCounterConfigSectionName));
            var perfcountersConfigSection = settings.Sections[PerfCounterConfigSectionName];

            Debug.Assert(perfcountersConfigSection.Parameters.Contains(PerfCounterFilePathParameterName));
            var perfCounters = perfcountersConfigSection.Parameters[PerfCounterFilePathParameterName];
            var perfCountersConfigJsonPath = Path.Combine(this.Context.CodePackageActivationContext.GetCodePackageObject("Code").Path, perfCounters.Value);

            var text = File.ReadAllText(perfCountersConfigJsonPath);
            var config = JsonConvert.DeserializeObject<PerformanceCounterServiceConfiguration>(text);

            return config.PerformanceCounters.Select(x =>
            {
                return new MonitorPerformanceCounter(x);
            }).ToList();
        }

        protected override Task OnCloseAsync(CancellationToken cancellationToken)
        {
            this._logger.Dispose();
            foreach (var counter in this._perfCounters)
                counter.Dispose();

            return base.OnCloseAsync(cancellationToken);
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
            while (!cancellationToken.IsCancellationRequested)
            {
                await ReportCountersAsync().ConfigureAwait(false);
                await Task.Delay(_checkInterval, cancellationToken).ConfigureAwait(false);
            }
        }

        private Task ReportCountersAsync()
        {
            lock (thisLock)
            {
                foreach (var counter in _perfCounters)
                {
                    //Can't await inside a lock statement. TODO: Refactor this
                    _logger.ReportMetricAsync(counter.FriendlyName, counter.NextValue()).Wait();
                }
            }

            return Task.FromResult(0);
        }
    }
}
