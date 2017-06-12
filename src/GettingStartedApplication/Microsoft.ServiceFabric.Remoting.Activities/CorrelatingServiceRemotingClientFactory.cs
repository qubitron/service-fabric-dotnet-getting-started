// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Remoting.Activities
{
    public abstract class CorrelatingServiceRemotingClientFactory : IServiceRemotingClientFactory
    {
        private IServiceRemotingClientFactory innerClientFactory;

        public CorrelatingServiceRemotingClientFactory(IServiceRemotingClientFactory innerClientFactory)
        {
            if (innerClientFactory == null)
            {
                throw new ArgumentNullException(nameof(innerClientFactory));
            }

            this.innerClientFactory = innerClientFactory;
            this.innerClientFactory.ClientConnected += this.ClientConnected;
            this.innerClientFactory.ClientDisconnected += this.ClientDisconnected;
        }

        public event EventHandler<CommunicationClientEventArgs<IServiceRemotingClient>> ClientConnected;
        public event EventHandler<CommunicationClientEventArgs<IServiceRemotingClient>> ClientDisconnected;

        public async Task<IServiceRemotingClient> GetClientAsync(Uri serviceUri, ServicePartitionKey partitionKey, TargetReplicaSelector targetReplicaSelector, 
            string listenerName, OperationRetrySettings retrySettings, CancellationToken cancellationToken)
        {
            var innerClient = await this.innerClientFactory.GetClientAsync(serviceUri, partitionKey, targetReplicaSelector, listenerName, retrySettings, cancellationToken);
            return new CorrelatingServiceRemotingClient(innerClient, serviceUri);
        }

        public async Task<IServiceRemotingClient> GetClientAsync(ResolvedServicePartition previousRsp, TargetReplicaSelector targetReplicaSelector, string listenerName, OperationRetrySettings retrySettings, CancellationToken cancellationToken)
        {
            var innerClient = await this.innerClientFactory.GetClientAsync(previousRsp, targetReplicaSelector, listenerName, retrySettings, cancellationToken);
            return new CorrelatingServiceRemotingClient(innerClient, previousRsp.ServiceName);
        }

        public Task<OperationRetryControl> ReportOperationExceptionAsync(IServiceRemotingClient client, ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, CancellationToken cancellationToken)
        {
            return this.innerClientFactory.ReportOperationExceptionAsync(client, exceptionInformation, retrySettings, cancellationToken);
        }
    }
}
