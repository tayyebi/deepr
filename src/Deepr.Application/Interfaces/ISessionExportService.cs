using Deepr.Application.DTOs;

namespace Deepr.Application.Interfaces;

public interface ISessionExportService
{
    Task<DecisionSheetExport?> ExportDecisionSheetAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
