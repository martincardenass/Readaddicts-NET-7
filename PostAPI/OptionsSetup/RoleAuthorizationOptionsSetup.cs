using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace PostAPI.OptionsSetup
{
    public class RoleAuthorizationOptionsSetup : IConfigureNamedOptions<AuthorizationOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public RoleAuthorizationOptionsSetup(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public void Configure(AuthorizationOptions options)
        {
            var authorizationOptions = _serviceProvider.GetRequiredService<AuthorizationOptions>();
            authorizationOptions.AddPolicy("AdminOnly", policy =>
            {
                policy.RequireRole("admin");
            });

            authorizationOptions.AddPolicy("UserAllowed", policy =>
            {
                policy.RequireRole("admin", "user");
            });

        }

        public void Configure(string? name, AuthorizationOptions options) => Configure(options);
    }
}
