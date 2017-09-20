// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

 // For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace WebService.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using StatelessBackendService.Interfaces;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.ServiceFabric.Remoting.Activities;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Client;

    [Route("api/[controller]")]
    public class StatelessBackendServiceController : Controller
    {
        private readonly ConfigSettings configSettings;
        private readonly StatelessServiceContext serviceContext;
        private readonly IServiceProxyFactory serviceProxyFactory;

        public StatelessBackendServiceController(StatelessServiceContext serviceContext, ConfigSettings settings)
        {
            this.serviceContext = serviceContext;
            this.configSettings = settings;
            this.serviceProxyFactory = new CorrelatingServiceProxyFactory(
                serviceContext,
                callbackClient => new FabricTransportServiceRemotingClientFactory(null, callbackClient, null, null, null)
                );
        }

        // GET: api/values
        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            string serviceUri = this.serviceContext.CodePackageActivationContext.ApplicationName + "/" + this.configSettings.StatelessBackendServiceName;
            IStatelessBackendService proxy = this.serviceProxyFactory.CreateServiceProxy<IStatelessBackendService>(new Uri(serviceUri));

            ServiceEventSource.Current.ServiceMessage(this.serviceContext, "In the web service about to call the backend!");

            long result = await proxy.GetCountAsync().ConfigureAwait(false);

            return this.Json(new CountViewModel() { Count = result });
        }
    }
}