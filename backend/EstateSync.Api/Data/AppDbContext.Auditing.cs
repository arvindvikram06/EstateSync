using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Models;
using EstateSync.Api.Common;

namespace EstateSync.Api.Data;

public partial class AppDbContext
{
    private List<AuditEntry> OnBeforeSave()
    {
        var auditEntries = new List<AuditEntry>();
        var entries = ChangeTracker.Entries().ToList();

        // 1. Handle BaseEntity fields (CreatedAt, UpdatedAt, DeletedAt soft delete)
        foreach (var entry in entries)
        {
            if (entry.Entity is BaseEntity baseEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    baseEntity.CreatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    baseEntity.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    baseEntity.DeletedAt = DateTime.UtcNow;
                }
            }
        }

        // 2. Generate Audit Logs
        foreach (var entry in entries)
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
            {
                continue;
            }

            var auditEntry = new AuditEntry(entry)
            {
                EntityName = entry.Metadata.DisplayName(),
                EntityType = entry.Entity.GetType().Name
            };
            auditEntries.Add(auditEntry);

            var keyName = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name;

            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;
                if (propertyName == keyName)
                {
                    auditEntry.EntityId = property.CurrentValue?.ToString();
                    if (property.IsTemporary)
                    {
                        auditEntry.TemporaryProperties.Add(property);
                    }
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.Action = "CREATE";
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        auditEntry.Action = "DELETE";
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.Action = "UPDATE";
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }
        }

        return auditEntries;
    }

    private void OnAfterSave(List<AuditEntry> auditEntries)
    {
        if (auditEntries == null || auditEntries.Count == 0)
        {
            return;
        }

        var ipAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                var keyName = auditEntry.Entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name;
                if (prop.Metadata.Name == keyName)
                {
                    auditEntry.EntityId = prop.CurrentValue?.ToString();
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            AuditLogs.Add(auditEntry.ToAuditLog(ipAddress));
        }

        base.SaveChanges();
    }

    private async Task OnAfterSaveAsync(List<AuditEntry> auditEntries)
    {
        if (auditEntries == null || auditEntries.Count == 0)
        {
            return;
        }

        var ipAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                var keyName = auditEntry.Entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name;
                if (prop.Metadata.Name == keyName)
                {
                    auditEntry.EntityId = prop.CurrentValue?.ToString();
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            AuditLogs.Add(auditEntry.ToAuditLog(ipAddress));
        }

        await base.SaveChangesAsync();
    }

    internal async Task<int> SaveSeedDataAsync(CancellationToken ct = default)
    {
        _isSaving = true;
        try { return await base.SaveChangesAsync(ct); }
        finally { _isSaving = false; }
    }

    public override int SaveChanges()
    {
        if (_isSaving) return base.SaveChanges();

        try
        {
            _isSaving = true;
            var auditEntries = OnBeforeSave();
            var result = base.SaveChanges();
            OnAfterSave(auditEntries);
            return result;
        }
        finally
        {
            _isSaving = false;
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        if (_isSaving) return base.SaveChanges(acceptAllChangesOnSuccess);

        try
        {
            _isSaving = true;
            var auditEntries = OnBeforeSave();
            var result = base.SaveChanges(acceptAllChangesOnSuccess);
            OnAfterSave(auditEntries);
            return result;
        }
        finally
        {
            _isSaving = false;
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_isSaving) return await base.SaveChangesAsync(cancellationToken);

        try
        {
            _isSaving = true;
            var auditEntries = OnBeforeSave();
            var result = await base.SaveChangesAsync(cancellationToken);
            await OnAfterSaveAsync(auditEntries);
            return result;
        }
        finally
        {
            _isSaving = false;
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (_isSaving) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        try
        {
            _isSaving = true;
            var auditEntries = OnBeforeSave();
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            await OnAfterSaveAsync(auditEntries);
            return result;
        }
        finally
        {
            _isSaving = false;
        }
    }
}
