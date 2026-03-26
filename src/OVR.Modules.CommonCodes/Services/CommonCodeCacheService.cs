using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OVR.Modules.CommonCodes.Persistence;
using OVR.SharedKernel.Contracts;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CommonCodes.Services;

public sealed class CommonCodeCacheService
    : ICommonCodeCache, IHostedService, INotificationHandler<CommonCodesReimportedEvent>
{
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ICommonCodeRepository? _directRepository;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CommonCodeEntry>> _cache = new();
    private readonly ConcurrentDictionary<string, string> _versions = new();

    public CommonCodeCacheService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    // For unit testing — avoids needing IServiceScopeFactory
    internal CommonCodeCacheService(ICommonCodeRepository repository) => _directRepository = repository;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var repository = GetRepository(out var scope);
        try
        {
            var types = await repository.GetDistinctTypesAsync(cancellationToken);
            foreach (var type in types)
                await LoadTypeFromAsync(repository, type, cancellationToken);
        }
        finally { scope?.Dispose(); }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public bool Exists(string type, string code) =>
        _cache.TryGetValue(type, out var codes) && codes.ContainsKey(code);

    public string? GetDescription(string type, string code, string language)
    {
        if (!_cache.TryGetValue(type, out var codes)) return null;
        if (!codes.TryGetValue(code, out var entry)) return null;
        return entry.Name.TryGetValue(language.ToLowerInvariant(), out var text) ? text.Long : null;
    }

    public IReadOnlyDictionary<string, CommonCodeEntry> GetByType(string type) =>
        _cache.TryGetValue(type, out var codes)
            ? codes
            : new ConcurrentDictionary<string, CommonCodeEntry>();

    public IReadOnlyList<string> GetAvailableTypes() =>
        _cache.Keys.ToList();

    public string GetVersion(string type) =>
        _versions.GetValueOrDefault(type, string.Empty);

    public async Task Handle(CommonCodesReimportedEvent notification, CancellationToken cancellationToken)
    {
        var repository = GetRepository(out var scope);
        try { await LoadTypeFromAsync(repository, notification.Type, cancellationToken); }
        finally { scope?.Dispose(); }
    }

    private ICommonCodeRepository GetRepository(out IServiceScope? scope)
    {
        if (_directRepository is not null) { scope = null; return _directRepository; }
        scope = _scopeFactory!.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ICommonCodeRepository>();
    }

    private async Task LoadTypeFromAsync(ICommonCodeRepository repository, string type, CancellationToken ct)
    {
        var documents = await repository.GetByTypeAsync(type, ct);
        var entries = new ConcurrentDictionary<string, CommonCodeEntry>();

        foreach (var doc in documents)
        {
            var name = doc.Name.ToDictionary(
                kvp => kvp.Key.ToLowerInvariant(),
                kvp => LocalizedText.Create(kvp.Value.Long, kvp.Value.Short));
            entries[doc.Code] = new CommonCodeEntry(doc.Code, doc.Order, name, doc.Attributes);
        }

        _cache[type] = entries;
        _versions[type] = ComputeHash(entries);
    }

    private static string ComputeHash(ConcurrentDictionary<string, CommonCodeEntry> entries)
    {
        var json = JsonSerializer.Serialize(
            entries.OrderBy(e => e.Key).Select(e => new { e.Key, e.Value.Order, Names = e.Value.Name.Keys.Order() }));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexStringLower(hash)[..16];
    }
}
