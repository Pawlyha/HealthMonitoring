﻿using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using HealthMonitoring.Management.Core.Registers;
using HealthMonitoring.Security;

namespace HealthMonitoring.SelfHost.Security
{
    public class AuthenticationFilter : IAuthenticationFilter
    {
        public ICredentialsProvider TokenProvider { get; set; }
        public IEndpointRegistry EndpointRegistry { get; set; }

        public bool AllowMultiple { get; }
        private const string _tokenKey = "PrivateToken";

        public AuthenticationFilter(IEndpointRegistry endpointRegistry, ICredentialsProvider tokenProvider)
        {
            TokenProvider = tokenProvider;
            EndpointRegistry = endpointRegistry;
        }

        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            IPrincipal principal = null;
            GenericIdentity identity;

            var credentials = context.ParseAuthorizationHeader();
            var adminCred = TokenProvider.GetAdminMonitorCredentials();
            var pullCred = TokenProvider.GetPullMonitorCredentials();

            if (credentials == null)
                return Task.FromResult(0);

            if (credentials.Equals(adminCred))
            {
                identity = new GenericIdentity(credentials.MonitorId.ToString());
                principal = new GenericPrincipal(identity, new[] { SecurityRole.AdminMonitor.ToString() });

            }else if (credentials.Equals(pullCred))
            {
                identity = new GenericIdentity(pullCred.MonitorId.ToString());
                principal = new GenericPrincipal(identity, new[] {SecurityRole.PullMonitor.ToString()});
            }
            else
            {
                string encryptedToken = credentials.PrivateToken.ToSha256Hash();
                var endpoint = EndpointRegistry.GetById(credentials.MonitorId);

                if (endpoint?.PrivateToken == encryptedToken)
                {
                    context.Request.Properties[_tokenKey] = encryptedToken;
                    identity = new GenericIdentity(credentials.MonitorId.ToString());
                    principal = new GenericPrincipal(identity, null);
                }
            }

            context.Principal = principal;

            return Task.FromResult(0);
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}
