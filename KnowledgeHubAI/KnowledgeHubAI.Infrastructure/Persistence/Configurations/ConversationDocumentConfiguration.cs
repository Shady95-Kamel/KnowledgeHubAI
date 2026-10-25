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
    public class ConversationDocumentConfiguration
     : IEntityTypeConfiguration<ConversationDocument>
    {
        public void Configure(
            EntityTypeBuilder<ConversationDocument> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Conversation)
                .WithMany(x => x.ConversationDocuments)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Document)
                .WithMany(x => x.ConversationDocuments)
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(x => new
            {
                x.ConversationId,
                x.DocumentId
            })
            .IsUnique();
        }
    }
}
