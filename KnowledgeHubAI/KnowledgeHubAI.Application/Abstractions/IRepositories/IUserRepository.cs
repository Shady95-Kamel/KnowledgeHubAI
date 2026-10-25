using KnowledgeHubAI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Application.Abstractions.IRepositories
{
    public interface IUserRepository
    {
        Task<bool> EmailExistsAsync(
            string email,
            CancellationToken cancellationToken);

        Task AddAsync(
            User user,
            CancellationToken cancellationToken);

        Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken);

    }
}
