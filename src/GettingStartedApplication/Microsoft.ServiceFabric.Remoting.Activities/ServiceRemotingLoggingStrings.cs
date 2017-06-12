// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Remoting.Activities
{
    public static class ServiceRemotingLoggingStrings
    {
        public const string InboundRequestActivityName = "Microsoft.ServiceFabric.Remoting.RemotingRequestIn";
        public const string RequestIdHeaderName = "Request-Id";
        public const string CorrelationContextHeaderName = "Correlation-Context";
        public const string DiagnosticListenerName = "ServiceRemotingClientDiagnosticListener";
        public const string OutboundRequestActivityName = "Microsoft.ServiceFabric.Remoting.RemotingRequestOut";
        public const string OutboundRequestActivityStartName = "Microsoft.ServiceFabric.Remoting.RemotingRequestOut.Start";
    }
}
