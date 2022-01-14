using System.Security.Claims;
using idunno.Authentication.Basic;

namespace arduino_ota_updater;

public static class BasicAuth
{
    public static Task ValidateCredentials(ValidateCredentialsContext context)
    {
        var validationService = context.HttpContext.RequestServices.GetService<IUserValidationService>();
        if (validationService!.AreCredentialsValid(context.Username, context.Password))
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                new Claim(ClaimTypes.Name, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer)
            };

            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
            context.Success();
        }

        return Task.CompletedTask;
    }
}
