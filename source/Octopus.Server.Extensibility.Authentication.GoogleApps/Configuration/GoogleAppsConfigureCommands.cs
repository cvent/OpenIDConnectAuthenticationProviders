﻿using System;
using System.Collections.Generic;
using Octopus.Server.Extensibility.Authentication.OpenIDConnect.Configuration;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Server.Extensibility.HostServices.Diagnostics;

namespace Octopus.Server.Extensibility.Authentication.GoogleApps.Configuration
{
    public class GoogleAppsConfigureCommands : OpenIdConnectConfigureCommands<IGoogleAppsConfigurationStore>
    {
        public GoogleAppsConfigureCommands(ILog log, Lazy<IGoogleAppsConfigurationStore> configurationStore) : base(log, configurationStore)
        {
        }

        protected override string ConfigurationSettingsName => "googleApps";

        public override IEnumerable<ConfigureCommandOption> GetOptions()
        {
            foreach (var option in base.GetOptions())
            {
                yield return option;
            }
            yield return new ConfigureCommandOption($"{ConfigurationSettingsName}CertificateUri=", $"Set the {ConfigurationSettingsName} CertificateUri", v =>
            {
                ConfigurationStore.Value.SetCertificateUri(v);
                Log.Info($"{ConfigurationSettingsName} CertificateUri set to: {v}");
            });
            yield return new ConfigureCommandOption($"{ConfigurationSettingsName}HostedDomain=", $"Set the {ConfigurationSettingsName} HostedDomain.", v =>
            {
                ConfigurationStore.Value.SetHostedDomain(v);
                Log.Info($"{ConfigurationSettingsName} HostedDomain set to: {v}");
            });
        }
    }
}