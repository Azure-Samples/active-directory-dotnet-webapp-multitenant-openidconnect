/************************************************************************************************
The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
***********************************************************************************************/

using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TodoListWebApp.Utils
{
    /// <summary>
    /// Generic class that validates token issuer from the provided Azure AD authority
    /// </summary>
    public static class AadIssuerValidator
    {
        /// <summary>
        /// Validate the issuer for multi-tenant applications of various audience (Work and School account, or Work and School accounts +
        /// Personal accounts)
        /// </summary>
        /// <param name="issuer">Issuer to validate (will be tenanted)</param>
        /// <param name="securityToken">Received Security Token</param>
        /// <param name="validationParameters">Token Validation parameters</param>
        /// <remarks>The issuer is considered as valid if it has the same http scheme and authority as the
        /// authority from the configuration file, has a tenant Id, and optionally v2.0 (this web api
        /// accepts both V1 and V2 tokens).
        /// Authority aliasing is also taken into account</remarks>
        /// <returns>The <c>issuer</c> if it's valid, or otherwise <c>SecurityTokenInvalidIssuerException</c> is thrown</returns>
        public static string ValidateAadIssuer(string issuer, SecurityToken securityToken, TokenValidationParameters validationParameters)
        {
            JwtSecurityToken jwtToken = securityToken as JwtSecurityToken;
            if (jwtToken == null)
            {
                throw new ArgumentNullException(nameof(securityToken), $"{nameof(securityToken)} cannot be null.");
            }

            if (validationParameters == null)
            {
                throw new ArgumentNullException(nameof(validationParameters), $"{nameof(validationParameters)} cannot be null.");
            }

            // Extract the tenant Id from the claims
            string tenantId = jwtToken.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new SecurityTokenInvalidIssuerException("The `tid` claim is not present in the token obtained from Azure Active Directory.");
            }

            // Build a list of valid tenanted issuers from the provided TokenValidationParameters.
            List<string> allValidTenantedIssuers = new List<string>();

            IEnumerable<string> validIssuers = validationParameters.ValidIssuers;
            if (validIssuers != null)
            {
                allValidTenantedIssuers.AddRange(validIssuers.Select(i => TenantedIssuer(i, tenantId)));
            }

            if (validationParameters.ValidIssuer != null)
            {
                allValidTenantedIssuers.Add(TenantedIssuer(validationParameters.ValidIssuer, tenantId));
            }

            // Consider the aliases (https://login.microsoftonline.com (v2.0 tokens) => https://sts.windows.net (v1.0 tokens) )
            allValidTenantedIssuers.AddRange(allValidTenantedIssuers.Select(i => i.Replace("https://login.microsoftonline.com", "https://sts.windows.net")).ToArray());

            // Consider tokens provided both by v1.0 and v2.0 issuers
            allValidTenantedIssuers.AddRange(allValidTenantedIssuers.Select(i => i.Replace("/v2.0", "/")).ToArray());

            if (!allValidTenantedIssuers.Contains(issuer))
            {
                throw new SecurityTokenInvalidIssuerException("Issuer does not match any of the valid issuers provided for this application.");
            }
            else
            {
                return issuer;
            }
        }

        private static string TenantedIssuer(string i, string tenantId)
        {
            return i.Replace("{tenantid}", tenantId);
        }
    }
}