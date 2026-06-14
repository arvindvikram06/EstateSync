using EstateSync.Api.Data;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Repositories;

public class OwnerSettlementRepository : BaseRepository<OwnerSettlement>, IOwnerSettlementRepository
{
    public OwnerSettlementRepository(AppDbContext context) : base(context)
    {
    }
}
