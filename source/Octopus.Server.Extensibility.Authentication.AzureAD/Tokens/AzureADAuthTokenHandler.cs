﻿using Microsoft.IdentityModel.Tokens;
using Octopus.Server.Extensibility.Authentication.AzureAD.Configuration;
using Octopus.Server.Extensibility.Authentication.OpenIDConnect.Certificates;
using Octopus.Server.Extensibility.Authentication.OpenIDConnect.Issuer;
using Octopus.Server.Extensibility.Authentication.OpenIDConnect.Tokens;

namespace Octopus.Server.Extensibility.Authentication.AzureAD.Tokens
{
    public class AzureADAuthTokenHandler : OpenIDConnectAuthTokenHandler<IAzureADConfigurationStore, ICertificateRetriever>, IAzureADAuthTokenHandler
    {
        public AzureADAuthTokenHandler(IAzureADConfigurationStore configurationStore, IIdentityProviderConfigDiscoverer identityProviderConfigDiscoverer, ICertificateRetriever certificateRetriever) : base(configurationStore, identityProviderConfigDiscoverer, certificateRetriever)
        {
        }

        protected override void SetIssuerSpecificTokenValidationParameters(TokenValidationParameters validationParameters)
        {
            if (!string.IsNullOrWhiteSpace(ConfigurationStore.GetRoleClaimType()))
                validationParameters.RoleClaimType = ConfigurationStore.GetRoleClaimType();
        }
    }
}