using KnowledgeHubAI.Domain.Enums;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using KnowledgeHubAI.SharedKernel.Entities;

namespace KnowledgeHubAI.Domain.Entities
{
    public class User : AuditableEntity
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string? PasswordHash { get; set; }

        public bool EmailConfirmed { get; set; }

        public UserRole Role { get; set; } = UserRole.User;

        public UserStatus Status { get; set; } = UserStatus.PendingVerification;

        public ICollection<RefreshToken> RefreshTokens { get; set; }
            = new List<RefreshToken>();

        public ICollection<ExternalLogin> ExternalLogins { get; set; }
            = new List<ExternalLogin>();

        public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; }
            = new List<EmailVerificationToken>();

        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; }
            = new List<PasswordResetToken>();

        public ICollection<Document> Documents { get; set; }
            = new List<Document>();

        public ICollection<Conversation> Conversations { get; set; }
            = new List<Conversation>();

        public ICollection<Notification> Notifications { get; set; }
            = new List<Notification>();
    }
}
