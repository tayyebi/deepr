using System.Net.Http.Json;
using Deepr.Web.Models;

namespace Deepr.Web.Services;

public class DeeprApiClient
{
    private readonly HttpClient _http;

    public DeeprApiClient(HttpClient http)
    {
        _http = http;
    }

    // Issues
    public Task<List<IssueDto>?> GetIssuesAsync() =>
        _http.GetFromJsonAsync<List<IssueDto>>("api/issues");

    public Task<IssueDto?> GetIssueAsync(Guid id) =>
        _http.GetFromJsonAsync<IssueDto>($"api/issues/{id}");

    public async Task<IssueDto?> CreateIssueAsync(string title, string contextVector, Guid ownerId)
    {
        var response = await _http.PostAsJsonAsync("api/issues", new { title, contextVector, ownerId });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IssueDto>();
    }

    // Councils
    public Task<CouncilDto?> GetCouncilAsync(Guid id) =>
        _http.GetFromJsonAsync<CouncilDto>($"api/councils/{id}");

    public async Task<CouncilDto?> CreateCouncilAsync(Guid issueId, int selectedMethod, int selectedTool)
    {
        var response = await _http.PostAsJsonAsync("api/councils", new { issueId, selectedMethod, selectedTool });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CouncilDto>();
    }

    public async Task<CouncilDto?> AddMemberAsync(Guid councilId, Guid agentId, string name, int role, bool isAi, string? systemPromptOverride = null)
    {
        var response = await _http.PostAsJsonAsync($"api/councils/{councilId}/members",
            new { agentId, name, role, isAi, systemPromptOverride });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CouncilDto>();
    }

    // Sessions
    public Task<SessionDto?> GetSessionAsync(Guid id) =>
        _http.GetFromJsonAsync<SessionDto>($"api/sessions/{id}");

    public async Task<SessionDto?> StartSessionAsync(Guid councilId)
    {
        var response = await _http.PostAsJsonAsync("api/sessions/start", new { councilId });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionDto>();
    }

    public async Task<SessionRoundDto?> ExecuteRoundAsync(Guid sessionId)
    {
        var response = await _http.PostAsync($"api/sessions/{sessionId}/execute-round", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionRoundDto>();
    }

    public async Task<string?> FinalizeSessionAsync(Guid sessionId)
    {
        var response = await _http.PostAsync($"api/sessions/{sessionId}/finalize", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> ExportDecisionSheetAsync(Guid sessionId)
    {
        var response = await _http.GetAsync($"api/sessions/{sessionId}/export");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsByteArrayAsync();
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? $"decision-sheet-{sessionId}.md";
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/markdown";
        return (content, fileName, contentType);
    }
}
