using KnowledgeHubAI.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Infrastructure.Services
{
    public sealed class TokenService : ITokenService
    {
        public string GenerateSecureToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(tokenBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public string HashToken(string token)
        {
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = SHA256.HashData(tokenBytes);

            return Convert.ToHexString(hashBytes);
        }
    }
}
