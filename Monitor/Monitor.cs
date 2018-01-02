using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Monitor.Shared.Logging;
using System.Diagnostics;
using System.Fabric.Description;
using Monitor.Shared.Logging.AppInsights;

namespace Monitor
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Monitor : StatelessService
    {
        //TODO: Make this configurable
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

        private const string AppInsightsConfigSectionName = "AppInsights";
        private const string AppInsightsTelemetryKeyConfigurationKey = "TelemetryKey";

        /// <summary>
        /// Logger for performance counter data
        /// </summary>
        private ILogger _logger = null;

        /// <summary>
        /// Lock for managed resources
        /// </summary>
        private Object thisLock = new Object();

        public Monitor(StatelessServiceContext context)
            : base(context)
        {
            _fabricClient = new FabricClient(FabricClientRole.User);
        }

        /// <summary>
        /// Service Fabric client instance.
        /// </summary>
        private static FabricClient _fabricClient = null;

        /// <summary>
        /// Refreshes the FabricClient instance.
        /// </summary>
        private void RefreshFabricClient()
        {
            FabricClient old = Interlocked.CompareExchange<FabricClient>(ref _fabricClient, new FabricClient(FabricClientRole.User), _fabricClient);
            old?.Dispose();
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
                    this._logger = InitializeLogger(configPackage.Settings);
                }
            }

            return base.OnOpenAsync(cancellationToken);
        }

        protected override Task OnCloseAsync(CancellationToken cancellationToken)
        {
            this._logger.Dispose();

            return base.OnCloseAsync(cancellationToken);
        }

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
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    //Check cluster health
                    var clusterHealth = await _fabricClient.HealthManager.GetClusterHealthAsync().ConfigureAwait(false);

                    //Report availability for each application
                    foreach(var applicationHealthState in clusterHealth.ApplicationHealthStates)
                    {
                        await ReportApplicationHealthStateAsync(applicationHealthState, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (FabricObjectClosedException)
                {
                    RefreshFabricClient();
                    ServiceEventSource.Current.ServiceMessage(this.Context, "FabricClient closed");
                }

                await Task.Delay(_checkInterval, cancellationToken);
            }
        }

        private Task ReportApplicationHealthStateAsync(System.Fabric.Health.ApplicationHealthState appHealthState, CancellationToken cancellationToken)
        {
            //TODO: Applications which are unhealthy - dive down to the service level and report those else where
            return _logger.ReportApplicationAvailabilityAsync(appHealthState.ApplicationName.AbsolutePath.TrimStart('\\'), "Monitor Watchdog", DateTimeOffset.UtcNow, appHealthState.AggregatedHealthState != System.Fabric.Health.HealthState.Error, cancellationToken);
        }
    }
}
