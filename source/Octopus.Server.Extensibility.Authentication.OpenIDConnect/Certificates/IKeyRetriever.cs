﻿using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Octopus.Server.Extensibility.Authentication.OpenIDConnect.Issuer;

namespace Octopus.Server.Extensibility.Authentication.OpenIDConnect.Certificates
{
    public interface IKeyRetriever
    {
        Task<IDictionary<string, AsymmetricSecurityKey>> GetCertificatesAsync(IssuerConfiguration issuerConfiguration);
    }
}