﻿using System;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.Authentication.GoogleApps.Configuration
{
    public class GoogleAppsConfigurationMapping : IConfigurationDocumentMapper
    {
        public Type GetTypeToMap() => typeof(GoogleAppsConfiguration);
    }
}