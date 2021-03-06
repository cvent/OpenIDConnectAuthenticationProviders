﻿using System;
using Octopus.Server.Extensibility.Authentication.GoogleApps.Configuration;
using Octopus.Server.Extensibility.Authentication.GoogleApps.Tokens;
using Octopus.Server.Extensibility.Authentication.GoogleApps.Web;
using Octopus.Server.Extensibility.Authentication.OpenIDConnect;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Api;

namespace Octopus.Server.Extensibility.Authentication.GoogleApps
{
    public class GoogleAppsApi : OpenIDConnectModule<GoogleAppsUserAuthenticationAction, IGoogleAppsConfigurationStore, GoogleAppsUserAuthenticatedAction, IGoogleAuthTokenHandler>
    {
        public GoogleAppsApi(
            IGoogleAppsConfigurationStore configurationStore, 
            GoogleAppsAuthenticationProvider authenticationProvider,
            Func<WhenEnabledAsyncActionInvoker<GoogleAppsUserAuthenticationAction, IGoogleAppsConfigurationStore>> authenticateUserActionFactory,
            Func<WhenEnabledAsyncActionInvoker<GoogleAppsUserAuthenticatedAction, IGoogleAppsConfigurationStore>> userAuthenticatedActionFactory) : base(configurationStore, authenticationProvider)
        {
            Post[authenticationProvider.AuthenticateUri, true] = async (_, token) => await authenticateUserActionFactory().ExecuteAsync(Context, Response);
            Post[configurationStore.RedirectUri, true] = async (_, token) => await userAuthenticatedActionFactory().ExecuteAsync(Context, Response);
        }
    }
}