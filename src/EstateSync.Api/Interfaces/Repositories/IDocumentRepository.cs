using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IDocumentRepository : IRepository<Document>
{
    Task<IEnumerable<Document>> GetByUploadedUserIdAsync(Guid userId);
    Task<(IEnumerable<Document> Items, int Total)> GetByStatusAsync(ApprovalStatus status, int page, int limit);
}
