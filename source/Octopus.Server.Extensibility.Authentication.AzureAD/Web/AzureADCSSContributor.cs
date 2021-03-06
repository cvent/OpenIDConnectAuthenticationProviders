﻿using Octopus.Server.Extensibility.Authentication.AzureAD.Configuration;
using Octopus.Server.Extensibility.Authentication.OpenIDConnect.Web;

namespace Octopus.Server.Extensibility.Authentication.AzureAD.Web
{
    public class AzureADCSSContributor : OpenIDConnectCSSContributor<IAzureADConfigurationStore>
    {
        public AzureADCSSContributor(IAzureADConfigurationStore configurationStore) : base(configurationStore)
        {
        }

        public override string CSSFilename => "azureAD";
    }
}