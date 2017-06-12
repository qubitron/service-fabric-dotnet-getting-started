// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ServiceFabric.Services.Remoting;
using System.Text;

namespace Microsoft.ServiceFabric.Remoting.Activities
{
    internal static class ServiceRemotingMessageHeadersExtensions
    {
        public static bool TryGetHeaderValue(this ServiceRemotingMessageHeaders messageHeaders, string headerName, out string headerValue)
        {
            headerValue = null;
            if (!messageHeaders.TryGetHeaderValue(headerName, out byte[] headerValueBytes))
            {
                return false;
            }

            headerValue = Encoding.UTF8.GetString(headerValueBytes);
            return true;
        }

        public static void AddHeader(this ServiceRemotingMessageHeaders messageHeaders, string headerName, string value)
        {
            messageHeaders.AddHeader(headerName, Encoding.UTF8.GetBytes(value));
        }
    }
}
