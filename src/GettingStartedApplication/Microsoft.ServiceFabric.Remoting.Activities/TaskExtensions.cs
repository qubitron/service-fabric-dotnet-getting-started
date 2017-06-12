// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Remoting.Activities
{
    internal static class TaskExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "task")]
        public static void Forget(this Task task) { }
    }
}
