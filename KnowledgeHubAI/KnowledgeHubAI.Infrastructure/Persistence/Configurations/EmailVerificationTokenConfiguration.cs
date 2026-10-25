using KnowledgeHubAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Infrastructure.Persistence.Configurations
{
    public class EmailVerificationTokenConfiguration
     : IEntityTypeConfiguration<EmailVerificationToken>
    {
        public void Configure(
            EntityTypeBuilder<EmailVerificationToken> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Token)
                .IsRequired();

            builder.HasIndex(x => x.Token)
                .IsUnique();

            builder.HasOne(x => x.User)
                .WithMany(x => x.EmailVerificationTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
