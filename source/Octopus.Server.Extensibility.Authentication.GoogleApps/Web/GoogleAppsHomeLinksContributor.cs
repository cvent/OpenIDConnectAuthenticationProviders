﻿using Octopus.Server.Extensibility.Authentication.GoogleApps.Configuration;
using Octopus.Server.Extensibility.Authentication.OpenIDConnect.Web;

namespace Octopus.Server.Extensibility.Authentication.GoogleApps.Web
{
    public class GoogleAppsHomeLinksContributor : OpenIDConnectHomeLinksContributor<IGoogleAppsConfigurationStore, GoogleAppsAuthenticationProvider>
    {
        public GoogleAppsHomeLinksContributor(IGoogleAppsConfigurationStore configurationStore, GoogleAppsAuthenticationProvider authenticationProvider) : base(configurationStore, authenticationProvider)
        {
        }
    }
}