using KnowledgeHubAI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Application.Abstractions.IRepositories
{
    public interface IEmailVerificationTokenRepository
    {
        Task AddAsync(
            EmailVerificationToken token,
            CancellationToken cancellationToken);

        Task<EmailVerificationToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken);
    }
}
