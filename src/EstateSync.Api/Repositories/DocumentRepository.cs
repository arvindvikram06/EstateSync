using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class DocumentRepository : BaseRepository<Document>, IDocumentRepository
{
    public DocumentRepository(AppDbContext context) : base(context)
    {
    }

    // Reserved for future modules — not yet called by any service
    // public async Task<IEnumerable<Document>> GetByUploadedUserIdAsync(Guid userId)
    // {
    //     return await _dbSet
    //         .AsNoTracking()
    //         .Where(d => d.UploadedById == userId)
    //         .ToListAsync();
    // }

    // public async Task<(IEnumerable<Document> Items, int Total)> GetByStatusAsync(ApprovalStatus status, int page, int limit)
    // {
    //     var query = _dbSet
    //         .AsNoTracking()
    //         .Where(d => d.ReviewStatus == status);

    //     var total = await query.CountAsync();
        
    //     var items = await query
    //         .OrderByDescending(d => d.UploadedAt)
    //         .Skip((page - 1) * limit)
    //         .Take(limit)
    //         .ToListAsync();

    //     return (items, total);
    // }
}
