using KnowledgeHubAI.Application.Abstractions.IRepositories;
using KnowledgeHubAI.Domain.Entities;
using KnowledgeHubAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<bool> EmailExistsAsync(
            string email,
            CancellationToken cancellationToken)
        {
            return _context.Users.AnyAsync(
                x => x.Email == email,
                cancellationToken);
        }

        public async Task AddAsync(
            User user,
            CancellationToken cancellationToken)
        {
            await _context.Users.AddAsync(user, cancellationToken);

        }


        public Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            return _context.Users.FirstOrDefaultAsync(
                x => x.Email == normalizedEmail,
                cancellationToken);
        }

    }
}
