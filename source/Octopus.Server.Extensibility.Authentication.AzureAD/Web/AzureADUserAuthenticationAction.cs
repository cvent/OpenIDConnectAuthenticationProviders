﻿using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Authentication.AzureAD.Configuration;
using Octopus.Server.Extensibility.Authentication.AzureAD.Issuer;
using Octopus.Server.Extensibility.Authentication.OpenIDConnect.Issuer;
using Octopus.Server.Extensibility.Authentication.OpenIDConnect.Web;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Api;
using Octopus.Server.Extensibility.HostServices.Web;

namespace Octopus.Server.Extensibility.Authentication.AzureAD.Web
{
    public class AzureADUserAuthenticationAction : UserAuthenticationAction<IAzureADConfigurationStore>
    {
        public AzureADUserAuthenticationAction(
            ILog log,
            IAzureADConfigurationStore configurationStore, 
            IIdentityProviderConfigDiscoverer identityProviderConfigDiscoverer, 
            IAzureADAuthorizationEndpointUrlBuilder urlBuilder,
            IApiActionResponseCreator responseCreator,
            IApiActionModelBinder modelBinder,
            IWebPortalConfigurationStore webPortalConfigurationStore) : base(log, configurationStore, identityProviderConfigDiscoverer, urlBuilder, responseCreator, modelBinder, webPortalConfigurationStore)
        {
        }
    }
}