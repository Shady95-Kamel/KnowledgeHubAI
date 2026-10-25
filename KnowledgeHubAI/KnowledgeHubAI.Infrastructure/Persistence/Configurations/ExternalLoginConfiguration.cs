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
    public class ExternalLoginConfiguration
    : IEntityTypeConfiguration<ExternalLogin>
    {
        public void Configure(EntityTypeBuilder<ExternalLogin> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Provider)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.ProviderUserId)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasMaxLength(255)
                .IsRequired();

            builder.HasIndex(x => new
            {
                x.Provider,
                x.ProviderUserId
            })
            .IsUnique();

            builder.HasIndex(x => new
            {
                x.Provider,
                x.Email
            });

            builder.HasOne(x => x.User)
                .WithMany(x => x.ExternalLogins)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
