﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StatelessBackendService
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using global::StatelessBackendService.Interfaces;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.ServiceFabric;

    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class StatelessBackendService : StatelessService, IStatelessBackendService
    {
        private long iterations = 0;
        private TelemetryClient telemetryClient;

        public StatelessBackendService(StatelessServiceContext context)
            : base(context)
        {
            var telemetryConfig = TelemetryConfiguration.Active;

            // Replace the fabric telemetry initializer, if there is one, with one that has the rich context
            for (int i = 0; i < telemetryConfig.TelemetryInitializers.Count; i++)
            {
                if (telemetryConfig.TelemetryInitializers[i] is FabricTelemetryInitializer)
                {
                    telemetryConfig.TelemetryInitializers[i] = FabricTelemetryInitializerExtension.CreateFabricTelemetryInitializer(context);
                    break;
                }
            }

            var config = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            var appInsights = config.Settings.Sections["ApplicationInsights"];
            telemetryConfig.InstrumentationKey = appInsights.Parameters["InstrumentationKey"].Value;

            this.telemetryClient = new TelemetryClient(telemetryConfig);
        }

        public async Task<long> GetCountAsync()
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, "In the backend service, getting the count!");
            long result = await Task.FromResult(this.iterations);
            if (result % 5 == 0)
            {
                throw new InvalidOperationException("Not happy with this number!");
            }
            return result;
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ++this.iterations;

                if (this.iterations % 50 == 1)
                {
                    // Raise "working" event only once in every 50 iterations
                    ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", this.iterations);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}