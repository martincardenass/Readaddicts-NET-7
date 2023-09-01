using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace PostAPI.OptionsSetup
{
    public class RoleAuthorizationOptionsSetup : IConfigureOptions<AuthorizationOptions>
    {
        public void Configure(AuthorizationOptions options)
        {
            // * Role based auth options
            options.AddPolicy("AdminOnly", policy =>
            {
                policy.AddAuthenticationSchemes("Bearer");
                policy.RequireAuthenticatedUser();
                policy.RequireRole("admin");
            });

            options.AddPolicy("UserAllowed", policy =>
            {
                policy.AddAuthenticationSchemes("Bearer");
                policy.RequireAuthenticatedUser();
                policy.RequireRole("admin", "user");
            });
        }
    }
}