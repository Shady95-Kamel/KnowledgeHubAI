using KnowledgeHubAI.Domain.Enums;
using KnowledgeHubAI.SharedKernel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeHubAI.Domain.Entities
{
    public class Document : AuditableEntity
    {
        public Guid UserId { get; set; }

        public string Name { get; set; } = null!;

        public string OriginalFileName { get; set; } = null!;

        public string BlobName { get; set; } = null!;

        public string ContentType { get; set; } = null!;

        public long FileSize { get; set; }

        public DocumentStatus Status { get; set; } = DocumentStatus.Uploading;

        public int? PageCount { get; set; }

        public User User { get; set; } = null!;

        public ICollection<ConversationDocument> ConversationDocuments { get; set; }
            = new List<ConversationDocument>();
    }
}
