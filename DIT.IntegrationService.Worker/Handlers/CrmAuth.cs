using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DIT.IntegrationService.Worker.Handlers
{
    public class CrmAuth
    {
        private readonly AdOption _azureAdOptions;
        private readonly AuthenticationContext _context;
        public CrmAuth(AdOption azureAdOptions)
        {
            _azureAdOptions = azureAdOptions;

            // Target deployment: CRM online => use OAuthMessageHandler
            ClientHandler = new OAuthMessageHandler(this, new HttpClientHandler());

            _context = new AuthenticationContext(_azureAdOptions.Instance + _azureAdOptions.TenantId);
        }

        public HttpMessageHandler ClientHandler { get; }

        #region Methods
        public async Task<AuthenticationResult> AcquireTokenAsync()
        {
            return await _context.AcquireTokenAsync(
                _azureAdOptions.OrganizationUrl,
                new ClientCredential(_azureAdOptions.ClientId, _azureAdOptions.ClientSecret));
        }
        #endregion

        #region OAuthMessageHandler for CRM Online (For On-Prem, use HttpClientHandler)
        class OAuthMessageHandler : DelegatingHandler
        {
            readonly CrmAuth _auth = null;

            public OAuthMessageHandler(CrmAuth auth, HttpMessageHandler innerHandler)
                : base(innerHandler)
            {
                _auth = auth;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                // It is a best practice to refresh the access token before every message request is sent. Doing so
                // avoids having to check the expiration date/time of the token. This operation is quick.
                var token = _auth.AcquireTokenAsync().Result.AccessToken;
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                return base.SendAsync(request, cancellationToken);
            }
        }
        #endregion
    }
}
