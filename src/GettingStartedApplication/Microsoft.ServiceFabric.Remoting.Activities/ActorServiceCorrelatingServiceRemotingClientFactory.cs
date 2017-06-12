// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ServiceFabric.Actors.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using System.Collections.Generic;

namespace Microsoft.ServiceFabric.Remoting.Activities
{
    public class ActorServiceCorrelatingServiceRemotingClientFactory: CorrelatingServiceRemotingClientFactory
    {
        public ActorServiceCorrelatingServiceRemotingClientFactory(
            FabricTransportRemotingSettings fabricTransportRemotingSettings,
            IServiceRemotingCallbackClient callbackClient,
            IServicePartitionResolver servicePartitionResolver = null,
            IEnumerable<IExceptionHandler> exceptionHandlers = null,
            string traceId = null)
        : base(new FabricTransportActorRemotingClientFactory(fabricTransportRemotingSettings, callbackClient, servicePartitionResolver, exceptionHandlers, traceId)) { }

        public ActorServiceCorrelatingServiceRemotingClientFactory(IServiceRemotingCallbackClient callbackClient)
        : base(new FabricTransportActorRemotingClientFactory(callbackClient)) { }
    }
}
