using Azure.Core;
using Azure.Identity;

namespace OneDriveBrowserApp.Authentication;

public interface ITokenCredentialProvider
{
    TokenCredential GetCredential();
}

public class InteractiveAuthenticationProvider : ITokenCredentialProvider
{
    public TokenCredential GetCredential()
    {
        const string tenantId = "common";
        const string clientId = "af2f834d-663f-400d-b724-aa778fdf2350";

        var options = new InteractiveBrowserCredentialOptions
        {
            TenantId = tenantId,
            ClientId = clientId,
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            // MUST be http://localhost or http://localhost:PORT
            // See https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/System-Browser-on-.Net-Core
            RedirectUri = new Uri("http://localhost"),
        };

        return new InteractiveBrowserCredential(options);
    }
}