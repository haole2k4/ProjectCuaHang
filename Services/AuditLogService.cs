using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Models;
using System.Text.Json;

namespace StoreManagementAPI.Services
{
    public interface IAuditLogService
    {
        Task LogActionAsync(string action, string entityType, int? entityId, string? entityName, 
            object? oldValues, object? newValues, string? changesSummary, 
            string? userId, string? username,
            Dictionary<string, object>? additionalInfo = null);
        
        Task<List<AuditLogDto>> GetLogsAsync(AuditLogFilterDto filter);
        Task<AuditLogDto?> GetLogByIdAsync(int auditId);
        Task<List<AuditLogDto>> GetLogsByEntityAsync(string entityType, int entityId);
        Task<List<AuditLogDto>> GetLogsByUserAsync(string userId);
        Task<AuditLogSummaryDto> GetSummaryAsync(DateTime? fromDate, DateTime? toDate);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly StoreDbContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(StoreDbContext context, ILogger<AuditLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActionAsync(string action, string entityType, int? entityId, string? entityName,
            object? oldValues, object? newValues, string? changesSummary,
            string? userId, string? username,
            Dictionary<string, object>? additionalInfo = null)
        {
            try
            {
                _logger.LogInformation(
                    "Logging audit action - Action: {Action}, EntityType: {EntityType}, EntityId: {EntityId}, UserId: {UserId}, Username: {Username}",
                    action, entityType, entityId, userId, username);

                var auditLog = new AuditLog
                {
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    EntityName = entityName,
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues, new JsonSerializerOptions { WriteIndented = true }) : null,
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues, new JsonSerializerOptions { WriteIndented = true }) : null,
                    ChangesSummary = changesSummary,
                    UserId = userId, // Allow null for failed logins or anonymous actions
                    Username = username ?? "anonymous",
                    AdditionalInfo = additionalInfo != null ? JsonSerializer.Serialize(additionalInfo, new JsonSerializerOptions { WriteIndented = true }) : null,
                    CreatedAt = DateTime.Now
                };

                _context.AuditLogs.Add(auditLog);

                _logger.LogInformation("Saving audit log to database...");
                await _context.SaveChangesAsync();
                _logger.LogInformation("Audit log saved successfully - AuditId: {AuditId}", auditLog.AuditId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error logging audit action - Action: {Action}, EntityType: {EntityType}, EntityId: {EntityId}, UserId: {UserId}, Username: {Username}. Error: {ErrorMessage}",
                    action, entityType, entityId, userId, username, ex.Message);
            }
        }

        public async Task<List<AuditLogDto>> GetLogsAsync(AuditLogFilterDto filter)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(filter.UserId))
                query = query.Where(a => a.UserId == filter.UserId);

            if (!string.IsNullOrWhiteSpace(filter.Username))
                query = query.Where(a => a.Username != null && a.Username.Contains(filter.Username));

            if (!string.IsNullOrWhiteSpace(filter.Action))
                query = query.Where(a => a.Action == filter.Action);

            if (!string.IsNullOrWhiteSpace(filter.EntityType))
                query = query.Where(a => a.EntityType == filter.EntityType);

            if (filter.EntityId.HasValue)
                query = query.Where(a => a.EntityId == filter.EntityId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(a => a.CreatedAt <= filter.ToDate.Value);

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new AuditLogDto
                {
                    AuditId = a.AuditId,
                    UserId = a.UserId,
                    Username = a.Username,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    EntityName = a.EntityName,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues,
                    ChangesSummary = a.ChangesSummary,
                    CreatedAt = a.CreatedAt,
                    AdditionalInfo = a.AdditionalInfo
                })
                .ToListAsync();

            return logs;
        }

        public async Task<AuditLogDto?> GetLogByIdAsync(int auditId)
        {
            var log = await _context.AuditLogs
                .Where(a => a.AuditId == auditId)
                .Select(a => new AuditLogDto
                {
                    AuditId = a.AuditId,
                    UserId = a.UserId,
                    Username = a.Username,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    EntityName = a.EntityName,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues,
                    ChangesSummary = a.ChangesSummary,
                    CreatedAt = a.CreatedAt,
                    AdditionalInfo = a.AdditionalInfo
                })
                .FirstOrDefaultAsync();

            return log;
        }

        public async Task<List<AuditLogDto>> GetLogsByEntityAsync(string entityType, int entityId)
        {
            var logs = await _context.AuditLogs
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AuditLogDto
                {
                    AuditId = a.AuditId,
                    UserId = a.UserId,
                    Username = a.Username,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    EntityName = a.EntityName,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues,
                    ChangesSummary = a.ChangesSummary,
                    CreatedAt = a.CreatedAt,
                    AdditionalInfo = a.AdditionalInfo
                })
                .ToListAsync();

            return logs;
        }

        public async Task<List<AuditLogDto>> GetLogsByUserAsync(string userId)
        {
            var logs = await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(100)
                .Select(a => new AuditLogDto
                {
                    AuditId = a.AuditId,
                    UserId = a.UserId,
                    Username = a.Username,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    EntityName = a.EntityName,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues,
                    ChangesSummary = a.ChangesSummary,
                    CreatedAt = a.CreatedAt,
                    AdditionalInfo = a.AdditionalInfo
                })
                .ToListAsync();

            return logs;
        }

        public async Task<AuditLogSummaryDto> GetSummaryAsync(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.CreatedAt <= toDate.Value);

            var logs = await query.ToListAsync();

            var summary = new AuditLogSummaryDto
            {
                TotalLogs = logs.Count,
                TotalUsers = logs.Where(a => !string.IsNullOrEmpty(a.UserId)).Select(a => a.UserId).Distinct().Count(),
                ActionCounts = logs.GroupBy(a => a.Action).ToDictionary(g => g.Key, g => g.Count()),
                EntityTypeCounts = logs.GroupBy(a => a.EntityType).ToDictionary(g => g.Key, g => g.Count()),
                RecentLogs = logs.OrderByDescending(a => a.CreatedAt).Take(10)
                    .Select(a => new AuditLogDto
                    {
                        AuditId = a.AuditId,
                        UserId = a.UserId,
                        Username = a.Username,
                        Action = a.Action,
                        EntityType = a.EntityType,
                        EntityId = a.EntityId,
                        EntityName = a.EntityName,
                        ChangesSummary = a.ChangesSummary,
                        CreatedAt = a.CreatedAt
                    }).ToList()
            };

            return summary;
        }
    }
}
