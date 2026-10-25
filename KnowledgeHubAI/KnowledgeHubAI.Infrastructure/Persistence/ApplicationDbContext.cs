using KnowledgeHubAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

        public DbSet<EmailVerificationToken> EmailVerificationTokens
            => Set<EmailVerificationToken>();

        public DbSet<PasswordResetToken> PasswordResetTokens
            => Set<PasswordResetToken>();

        public DbSet<Document> Documents => Set<Document>();

        public DbSet<Conversation> Conversations => Set<Conversation>();

        public DbSet<Message> Messages => Set<Message>();

        public DbSet<ConversationDocument> ConversationDocuments
            => Set<ConversationDocument>();

        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(ApplicationDbContext).Assembly);
        }
    }
}
