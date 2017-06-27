﻿using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Octopus.Data.Model.User;
using Octopus.DataCenterManager.Extensibility.Authentication.OpenIDConnect.Tokens;
using Octopus.Node.Extensibility.Authentication.HostServices;
using Octopus.Node.Extensibility.Authentication.OpenIDConnect.Configuration;
using Octopus.Node.Extensibility.Authentication.OpenIDConnect.Infrastructure;
using Octopus.Node.Extensibility.Authentication.Storage.User;
using Octopus.Node.Extensibility.HostServices.Web;
using Octopus.Time;

namespace Octopus.DataCenterManager.Extensibility.Authentication.OpenIDConnect.Web
{
    public abstract class AuthenticatedController<TStore, TAuthTokenHandler> : Controller
        where TStore : IOpenIDConnectConfigurationStore
        where TAuthTokenHandler : IAuthTokenHandler
    {
        readonly TAuthTokenHandler authTokenHandler;
        readonly IPrincipalToUserResourceMapper principalToUserResourceMapper;
        readonly IUpdateableUserStore userStore;
        readonly TStore configurationStore;
        readonly IInvalidLoginTracker loginTracker;
        readonly IUrlEncoder urlEncoder;
        readonly ISleep sleep;
        readonly IClock clock;

        protected AuthenticatedController(
            TAuthTokenHandler authTokenHandler,
            IPrincipalToUserResourceMapper principalToUserResourceMapper,
            IUpdateableUserStore userStore,
            TStore configurationStore,
            IInvalidLoginTracker loginTracker,
            IUrlEncoder urlEncoder,
            ISleep sleep,
            IClock clock)
        {
            this.authTokenHandler = authTokenHandler;
            this.principalToUserResourceMapper = principalToUserResourceMapper;
            this.userStore = userStore;
            this.configurationStore = configurationStore;
            this.loginTracker = loginTracker;
            this.urlEncoder = urlEncoder;
            this.sleep = sleep;
            this.clock = clock;
        }
        
        protected abstract string ProviderName { get; }
        
        protected async Task<IActionResult> ProcessAuthenticated()
        {
            string stateFromRequest;
            var principalContainer = await authTokenHandler.GetPrincipalAsync(Request.Form, out stateFromRequest);
            var principal = principalContainer.principal;
            if (principal == null || !string.IsNullOrEmpty(principalContainer.error))
            {
                return BadRequest($"The response from the external identity provider contained an error: {principalContainer.error}");
            }

            // Step 2: Validate the state object we passed wasn't tampered with
            const string stateDescription = "As a security precaution, Octopus ensures the state object returned from the external identity provider matches what it expected.";
            var expectedStateHash = string.Empty;
            if (Request.Cookies.ContainsKey("s"))
                expectedStateHash = urlEncoder.UrlDecode(Request.Cookies["s"]);
            if (string.IsNullOrWhiteSpace(expectedStateHash))
            {
                return BadRequest($"User login failed: Missing State Hash Cookie. {stateDescription} In this case the Cookie containing the SHA256 hash of the state object is missing from the request.");
            }

            var stateFromRequestHash = State.Protect(stateFromRequest);
            if (stateFromRequestHash != expectedStateHash)
            {
                return BadRequest($"User login failed: Tampered State. {stateDescription} In this case the state object looks like it has been tampered with. The state object is '{stateFromRequest}'. The SHA256 hash of the state was expected to be '{expectedStateHash}' but was '{stateFromRequestHash}'.");
            }

            // Step 3: Validate the nonce is as we expected to prevent replay attacks
            const string nonceDescription = "As a security precaution to prevent replay attacks, Octopus ensures the nonce returned in the claims from the external identity provider matches what it expected.";

            var expectedNonceHash = string.Empty;
            if (Request.Cookies.ContainsKey("n"))
                expectedNonceHash = urlEncoder.UrlDecode(Request.Cookies["n"]);

            if (string.IsNullOrWhiteSpace(expectedNonceHash))
            {
                return BadRequest($"User login failed: Missing Nonce Hash Cookie. {nonceDescription} In this case the Cookie containing the SHA256 hash of the nonce is missing from the request.");
            }

            var nonceFromClaims = principal.Claims.FirstOrDefault(c => c.Type == "nonce");
            if (nonceFromClaims == null)
            {
                return BadRequest($"User login failed: Missing Nonce Claim. {nonceDescription} In this case the 'nonce' claim is missing from the security token.");
            }

            var nonceFromClaimsHash = Nonce.Protect(nonceFromClaims.Value);
            if (nonceFromClaimsHash != expectedNonceHash)
            {
                return BadRequest($"User login failed: Tampered Nonce. {nonceDescription} In this case the nonce looks like it has been tampered with or reused. The nonce is '{nonceFromClaims}'. The SHA256 hash of the state was expected to be '{expectedNonceHash}' but was '{nonceFromClaimsHash}'.");
            }

            // Step 4: Now the integrity of the request has been validated we can figure out which Octopus User this represents
            var authenticationCandidate = principalToUserResourceMapper.MapToUserResource(principal);

            // Step 4a: Check if this authentication attempt is already being banned
            var action = loginTracker.BeforeAttempt(authenticationCandidate.Username, Request.HttpContext.Connection.RemoteIpAddress.ToString());
            if (action == InvalidLoginAction.Ban)
            {
                return BadRequest("You have had too many failed login attempts in a short period of time. Please try again later.");
            }

            // Step 4b: Try to get or create a the Octopus User this external identity represents
            var userResult = GetOrCreateUser(authenticationCandidate);
            if (userResult.Succeeded)
            {
                var groups = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
                if (groups.Any())
                {
                    userStore.SetSecurityGroupIds(ProviderName, userResult.User.Id, groups, clock.GetUtcTime());
                }

                loginTracker.RecordSucess(authenticationCandidate.Username, Request.HttpContext.Connection.RemoteIpAddress.ToString());

                return Redirect(stateFromRequest);
            }

            // Step 5: Handle other types of failures
            loginTracker.RecordFailure(authenticationCandidate.Username, Request.HttpContext.Connection.RemoteIpAddress.ToString());

            // Step 5a: Slow this potential attacker down a bit since they seem to keep failing
            if (action == InvalidLoginAction.Slow)
            {
                sleep.For(1000);
            }

            if (!userResult.User.IsActive)
            {
                return BadRequest($"The Octopus User Account '{authenticationCandidate.Username}' has been disabled by an Administrator. If you believe this to be a mistake, please contact your Octopus Administrator to have your account re-enabled.");
            }

            if (userResult.User.IsService)
            {
                return BadRequest($"The Octopus User Account '{authenticationCandidate.Username}' is a Service Account, which are prevented from using Octopus interactively. Service Accounts are designed to authorize external systems to access the Octopus API using an API Key.");
            }

            return BadRequest($"User login failed: {userResult.FailureReason}");
        }

        UserCreateResult GetOrCreateUser(UserResource userResource)
        {
            var user = userStore.GetByIdentity(new OAuthIdentity(ProviderName, userResource.EmailAddress,
                userResource.ExternalId));

            if (user != null)
            {
                var identity = user.Identities.OfType<OAuthIdentity>().FirstOrDefault(x => x.Provider != ProviderName);
                if (identity == null)
                {
                    return new UserCreateResult(userStore.AddIdentity(user.Id, NewIdentity(userResource)));
                }
                return new UserCreateResult(user);
            }

            if (!configurationStore.GetAllowAutoUserCreation())
                return new AuthenticationUserCreateResult("User could not be located and auto user creation is not enabled.");

            var userResult = userStore.Create(
                userResource.Username,
                userResource.DisplayName,
                userResource.EmailAddress,
                new[] { NewIdentity(userResource) });

            return userResult;
        }

        OAuthIdentity NewIdentity(UserResource userResource)
        {
            return new OAuthIdentity(ProviderName, userResource.EmailAddress, userResource.ExternalId);
        }

    }
}