// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Client;
using System.Collections.Generic;

namespace Microsoft.ServiceFabric.Remoting.Activities
{
    public class ReliableServiceCorrelatingServiceRemotingClientFactory : CorrelatingServiceRemotingClientFactory
    {
        public ReliableServiceCorrelatingServiceRemotingClientFactory(
            FabricTransportRemotingSettings fabricTransportRemotingSettings = null,
            IServiceRemotingCallbackClient callbackClient = null,
            IServicePartitionResolver servicePartitionResolver = null,
            IEnumerable<IExceptionHandler> exceptionHandlers = null,
            string traceId = null)
        : base(new FabricTransportServiceRemotingClientFactory(fabricTransportRemotingSettings, callbackClient, servicePartitionResolver, exceptionHandlers, traceId)) { }
    }
}
