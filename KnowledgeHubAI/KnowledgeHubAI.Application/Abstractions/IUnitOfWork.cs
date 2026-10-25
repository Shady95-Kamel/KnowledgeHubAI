using KnowledgeHubAI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Application.Abstractions
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default);

        Task AddRefreshTokenAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default);
    }

}
