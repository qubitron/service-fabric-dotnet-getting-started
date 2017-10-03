// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Remoting.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.ServiceFabric;
using Microsoft.ApplicationInsights.ServiceFabric.Remoting.Activities;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;

namespace ActorBackendService
{
    internal class MyActorService: ActorService
    {
        public MyActorService(
            StatefulServiceContext context,
            ActorTypeInformation actorTypeInfo,
            Func<ActorService, ActorId, ActorBase> actorFactory = null,
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null,
            IActorStateProvider stateProvider = null,
            ActorServiceSettings settings = null)
        : base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
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
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(
                    context => new FabricTransportActorServiceRemotingListener(
                        context,
                        new CorrelatingRemotingMessageHandler(this),
                        new FabricTransportRemotingListenerSettings()))
            };
        }
    }
}
