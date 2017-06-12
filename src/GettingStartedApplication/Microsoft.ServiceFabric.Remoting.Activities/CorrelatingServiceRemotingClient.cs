// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.ServiceFabric.Remoting.Activities
{
    public class CorrelatingServiceRemotingClient : IServiceRemotingClient
    {
        private static readonly DiagnosticListener s_diagnosticListener = new DiagnosticListener(ServiceRemotingLoggingStrings.DiagnosticListenerName);

        private IServiceRemotingClient innerClient;
        private Uri serviceUri;
        private Lazy<DataContractSerializer> baggageSerializer;
        private TelemetryClient telemetryClient;

        public CorrelatingServiceRemotingClient(IServiceRemotingClient innerClient, Uri serviceUri)
        {
            if (innerClient == null)
            {
                throw new ArgumentNullException(nameof(innerClient));
            }
            if (serviceUri == null)
            {
                throw new ArgumentNullException(nameof(serviceUri));
            }

            this.innerClient = innerClient;
            this.serviceUri = serviceUri;
            this.baggageSerializer = new Lazy<DataContractSerializer>(() => new DataContractSerializer(typeof(IEnumerable<KeyValuePair<string, string>>)));
            this.telemetryClient = new TelemetryClient();
        }

        public ResolvedServicePartition ResolvedServicePartition { get => this.innerClient.ResolvedServicePartition; set => this.innerClient.ResolvedServicePartition = value; }

        public string ListenerName { get => this.innerClient.ListenerName; set => this.innerClient.ListenerName = value; }

        public ResolvedServiceEndpoint Endpoint { get => this.innerClient.Endpoint; set => this.innerClient.Endpoint = value; }

        public Task<byte[]> RequestResponseAsync(ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            return SendAndTrackRequestAsync(messageHeaders, requestBody, () => this.innerClient.RequestResponseAsync(messageHeaders, requestBody));
        }

        public void SendOneWay(ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            SendAndTrackRequestAsync(messageHeaders, requestBody, () =>
            {
                this.innerClient.SendOneWay(messageHeaders, requestBody);
                return Task.FromResult<byte[]>(null);
            }).Forget();
        }

        private async Task<byte[]> SendAndTrackRequestAsync(ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody, Func<Task<byte[]>> doSendRequest)
        {
            (DependencyTelemetry dt, Activity activity) = SetUpDependencyCall(messageHeaders, requestBody);

            var operation = telemetryClient.StartOperation<DependencyTelemetry>(dt);
            bool success = true;
            byte[] retval = null;
            try
            {
                retval = await doSendRequest();
                return retval;
            }
            catch (Exception e)
            {
                success = false;
                telemetryClient.TrackException(e);
                throw;
            }
            finally
            {
                dt.Success = success;

                //always stop activity if it was started
                if (activity != null)
                {
                    s_diagnosticListener.StopActivity(activity, new { Response = retval, Headers = messageHeaders, RequestBody = requestBody });
                }

                telemetryClient.StopOperation(operation);
            }
        }

        private (DependencyTelemetry, Activity) SetUpDependencyCall(ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            DependencyTelemetry dt = new DependencyTelemetry();
            dt.Target = this.serviceUri.ToString();
            Activity activity = null;

            if (s_diagnosticListener.IsEnabled(ServiceRemotingLoggingStrings.OutboundRequestActivityName, messageHeaders, requestBody))
            {
                activity = new Activity(ServiceRemotingLoggingStrings.OutboundRequestActivityName);
                //Only send start event to users who subscribed for it, but start activity anyway
                if (s_diagnosticListener.IsEnabled(ServiceRemotingLoggingStrings.OutboundRequestActivityStartName))
                {
                    s_diagnosticListener.StartActivity(activity, new { Headers = messageHeaders, RequestBody = requestBody });
                }
                else
                {
                    activity.Start();
                }
            }

            Activity currentActivity = Activity.Current;
            if (currentActivity != null)
            {
                messageHeaders.AddHeader(ServiceRemotingLoggingStrings.RequestIdHeaderName, currentActivity.Id);

                // We expect the baggage to not be there at all or just contain a few small items
                if (currentActivity.Baggage.Any())
                {
                    using (var ms = new MemoryStream())
                    {
                        var dictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(ms);
                        this.baggageSerializer.Value.WriteObject(dictionaryWriter, currentActivity.Baggage);
                        dictionaryWriter.Flush();
                        messageHeaders.AddHeader(ServiceRemotingLoggingStrings.CorrelationContextHeaderName, ms.GetBuffer());
                    }
                }
            }

            return (dt, activity);
        }
    }
}
