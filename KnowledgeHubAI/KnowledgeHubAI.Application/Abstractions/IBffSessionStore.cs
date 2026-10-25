using KnowledgeHubAI.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Application.Abstractions
{
    public interface IBffSessionStore
    {
        Task<string> CreateAsync(
            BffSessionData session,
            CancellationToken cancellationToken = default);

        Task<BffSessionData?> GetAsync(
            string sessionId,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            string sessionId,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(
            string sessionId,
            BffSessionData session,
            CancellationToken cancellationToken = default);
    }
}
