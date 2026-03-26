using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using OVR.SharedKernel.Contracts;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CommonCodes.Features.BulkGetCommonCodes;

public sealed record BulkCommonCodesResponse(
    string Version,
    Dictionary<string, BulkTypeResponse> Types);

public sealed record BulkTypeResponse(
    string Version,
    Dictionary<string, BulkCodeEntry> Codes);

public sealed record BulkCodeEntry(
    int Order,
    Dictionary<string, BulkLocalizedText> Name,
    Dictionary<string, string> Attributes);

public sealed record BulkLocalizedText(string Long, string? Short);

public static class BulkGetCommonCodesHandler
{
    public static BulkCommonCodesResponse BuildResponse(
        ICommonCodeCache cache, string[]? types, string[]? languages)
    {
        var requestedTypes = types ?? cache.GetAvailableTypes().ToArray();
        var result = new Dictionary<string, BulkTypeResponse>();

        foreach (var type in requestedTypes)
        {
            var entries = cache.GetByType(type);
            var codes = new Dictionary<string, BulkCodeEntry>();

            foreach (var (code, entry) in entries)
            {
                var filteredName = FilterLanguages(entry.Name, languages);
                codes[code] = new BulkCodeEntry(entry.Order, filteredName, entry.Attributes.ToDictionary());
            }

            result[type] = new BulkTypeResponse(cache.GetVersion(type), codes);
        }

        var globalVersion = ComputeGlobalVersion(result);
        return new BulkCommonCodesResponse(globalVersion, result);
    }

    private static Dictionary<string, BulkLocalizedText> FilterLanguages(
        IReadOnlyDictionary<string, LocalizedText> name, string[]? languages)
    {
        var source = languages is { Length: > 0 }
            ? name.Where(kvp => languages.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
            : name;

        return source.ToDictionary(
            kvp => kvp.Key,
            kvp => new BulkLocalizedText(kvp.Value.Long, kvp.Value.Short));
    }

    private static string ComputeGlobalVersion(Dictionary<string, BulkTypeResponse> types)
    {
        var combined = string.Join("|", types.OrderBy(t => t.Key).Select(t => $"{t.Key}:{t.Value.Version}"));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexStringLower(hash)[..16];
    }
}

public static class BulkGetCommonCodesEndpoint
{
    public static IEndpointRouteBuilder MapBulkGetCommonCodesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/common-codes/bulk", Handle)
            .WithName("BulkGetCommonCodes")
            .WithTags("CommonCodes");

        return app;
    }

    private static IResult Handle(
        string? types,
        string? languages,
        ICommonCodeCache cache,
        HttpContext httpContext)
    {
        var typeArray = types?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var langArray = languages?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var response = BulkGetCommonCodesHandler.BuildResponse(cache, typeArray, langArray);

        // ETag / 304 support
        var etag = new EntityTagHeaderValue($"\"{response.Version}\"");
        if (httpContext.Request.Headers.IfNoneMatch.ToString() == etag.ToString())
            return Results.StatusCode(304);

        httpContext.Response.Headers.ETag = etag.ToString();
        httpContext.Response.Headers.CacheControl = "no-cache";

        return Results.Ok(response);
    }
}
