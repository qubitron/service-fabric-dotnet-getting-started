// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ServiceFabric.Actors.Remoting.Runtime;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.ServiceFabric.Remoting.Activities
{
    public class CorrelatingRemotingMessageHandler : IServiceRemotingMessageHandler
    {
        private Lazy<DataContractSerializer> baggageSerializer;

        private IServiceRemotingMessageHandler innerHandler;
        private TelemetryClient telemetryClient;

        public static AsyncLocal<int> operationKey = new AsyncLocal<int>();
        private static ConcurrentDictionary<int, string> _operationNames = new ConcurrentDictionary<int, string>();

        public CorrelatingRemotingMessageHandler(ServiceContext serviceContext, IService service)
        {
            this.innerHandler = new ServiceRemotingDispatcher(serviceContext, service);
            Initialize();
        }

        public CorrelatingRemotingMessageHandler(ActorService actorService)
        {
            this.innerHandler = new ActorServiceRemotingDispatcher(actorService);
            Initialize();
        }

        public void HandleOneWay(IServiceRemotingRequestContext requestContext, ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            HandleAndTrackRequestAsync(messageHeaders, () =>
            {
                this.innerHandler.HandleOneWay(requestContext, messageHeaders, requestBody);
                return Task.FromResult<byte[]>(null);
            }).Forget();
        }

        public Task<byte[]> RequestResponseAsync(IServiceRemotingRequestContext requestContext, ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            return HandleAndTrackRequestAsync(messageHeaders, () => {
                var result = this.innerHandler.RequestResponseAsync(requestContext, messageHeaders, requestBody);
                return result;
            });

        }

        private void Initialize()
        {
            this.telemetryClient = new TelemetryClient();
            this.baggageSerializer = new Lazy<DataContractSerializer>(() => new DataContractSerializer(typeof(IEnumerable<KeyValuePair<string, string>>)));
        }

        public class CorrelationData
        {
            public string operationName;
        }

        public static void SetCurrentOperationName(string operationName)
        {
            _operationNames[CorrelatingRemotingMessageHandler.operationKey.Value] = operationName;
        }

        private async Task<byte[]> HandleAndTrackRequestAsync(ServiceRemotingMessageHeaders messageHeaders, Func<Task<byte[]>> doHandleRequest)
        {
            RequestTelemetry rt = SetUpRequestActivity(messageHeaders);
            var rand = new Random();
            int key = rand.Next();
            while (!_operationNames.TryAdd(key, "Unknown"))
            {
                key = rand.Next();
            }
            operationKey.Value = key;
            bool success = true;
            try
            {
                byte[] retval = await doHandleRequest();
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
                Activity.Current.Stop();
                
                rt.Stop(Stopwatch.GetTimestamp());
                rt.Success = success;

                string operationName;
                if (_operationNames.TryRemove(key, out operationName))
                {
                    rt.Name = operationName;
                }

                telemetryClient.TrackRequest(rt);
            }
        }

        private RequestTelemetry SetUpRequestActivity(ServiceRemotingMessageHeaders messageHeaders)
        {
            var activity = new Activity(ServiceRemotingLoggingStrings.InboundRequestActivityName);
            RequestTelemetry rt = new RequestTelemetry();

            if (messageHeaders.TryGetHeaderValue(ServiceRemotingLoggingStrings.RequestIdHeaderName, out string requestId))
            {
                activity.SetParentId(requestId);
                rt.Context.Operation.ParentId = requestId;

                if (messageHeaders.TryGetHeaderValue(ServiceRemotingLoggingStrings.CorrelationContextHeaderName, out byte[] correlationBytes))
                {
                    var baggageBytesStream = new MemoryStream(correlationBytes, writable: false);
                    var dictionaryReader = XmlDictionaryReader.CreateBinaryReader(baggageBytesStream, XmlDictionaryReaderQuotas.Max);
                    var baggage = this.baggageSerializer.Value.ReadObject(dictionaryReader) as IEnumerable<KeyValuePair<string, string>>;
                    foreach (KeyValuePair<string, string> pair in baggage)
                    {
                        activity.AddBaggage(pair.Key, pair.Value);
                    }

                }
            }

            activity.Start();

            rt.Id = activity.Id;
            rt.Context.Operation.Id = activity.RootId;

            // TODO: this should be really service interface and method name, but ServiceRemotingMessageHeaders does not expose this information.
            rt.Name = ServiceRemotingLoggingStrings.InboundRequestActivityName + "(" + messageHeaders.InterfaceId.ToString() + "," + messageHeaders.MethodId.ToString() + ")";

            telemetryClient.Initialize(rt);
            rt.Start(Stopwatch.GetTimestamp());
            return rt;
        }
    }
}
