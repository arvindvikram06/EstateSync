using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.AuditLogs;
using EstateSync.Api.Interfaces.Services.AuditLogs;
using EstateSync.Api.Mappers;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.AuditLogs;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly AuditLogMapper _mapper;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        AuditLogMapper mapper,
        ILogger<AuditLogService> logger)
    {
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResponse<AuditLogResponse>> GetPagedLogsAsync(
        string? entityType,
        string? entityId,
        string? action,
        int page,
        int limit,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Fetching paginated audit logs. Page: {Page}, Limit: {Limit}, EntityType: {EntityType}, Action: {Action}",
            page, limit, entityType, action);

        var (items, total) = await _auditLogRepository.GetPagedAsync(entityType, entityId, action, page, limit);

        return new PagedResponse<AuditLogResponse>
        {
            Success = true,
            Data = items.Select(_mapper.ToResponse).ToList(),
            Page = page,
            Limit = limit,
            Total = total
        };
    }
}
