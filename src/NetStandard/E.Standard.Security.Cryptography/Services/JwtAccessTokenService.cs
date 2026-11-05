using System;
using System.Collections.Generic;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using E.Standard.Extensions.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace E.Standard.Security.Cryptography.Services;

public class JwtAccessTokenService
{
    private const string DefaultIssuer = "webgis";
    private SymmetricSecurityKey _key;
    private TokenValidationParameters _validationParameters;

    private JwtAccessTokenService() { }
    public JwtAccessTokenService(IOptions<CryptoServiceOptions> options)
    {
        var cryptoOptions = options.Value;

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cryptoOptions.DefaultPassword));
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = DefaultIssuer,

            ValidateAudience = false,
            //ValidAudience = audience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
        };
    }

    public const string PortalLoginAudience = "webgis-portal-login";

    public string GenerateToken(string userName, int lifeTimeMinutes, string audience = null)
    {
        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = new List<Claim>()
        {
            new Claim(ClaimTypes.Name, userName)
        };

        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(lifeTimeMinutes),
            SigningCredentials = credentials,
            Issuer = DefaultIssuer,
            Audience = String.IsNullOrEmpty(audience) ? null : audience,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(securityToken);
    }

    public ClaimsPrincipal ValidateToken(string token, string requiredAudience = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(
                                token,
                                _validationParameters,
                                out SecurityToken validatedToken
                            );

            if (validatedToken is JwtSecurityToken jwtToken)
            {
                if (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token algorithm");
                }

                if (!String.IsNullOrEmpty(requiredAudience)
                    && !jwtToken.Audiences.Contains(requiredAudience))
                {
                    throw new SecurityTokenException("Invalid token audience");
                }
            }
            else if (!String.IsNullOrEmpty(requiredAudience))
            {
                throw new SecurityTokenException("Invalid token audience");
            }

            return principal;
        }
        catch (Exception ex)
        {
            throw new SecurityTokenException("Token validation failed", ex);
        }
    }

    public string ValidatedName(string token, string requiredAudience = null)
        => ValidateToken(token, requiredAudience).Identity.Name;

    static public JwtAccessTokenService Create(string signingPassword)
    {
        var symKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(signingPassword.DoubleToMinLength(32)));

        return new JwtAccessTokenService()
        {
            _key = symKey,
            _validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = DefaultIssuer,

                ValidateAudience = false,
                //ValidAudience = audience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = symKey,
            }
        };
    }
}
