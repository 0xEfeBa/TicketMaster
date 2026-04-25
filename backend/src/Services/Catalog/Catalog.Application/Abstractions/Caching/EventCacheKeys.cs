namespace Catalog.Application.Abstractions.Caching;

/// <summary>
/// Yayın/iptal sonrası detay + yayın listesi (ilk sayfalar) tutarlılığı.
/// </summary>
public static class EventCacheKeys
{
    public static IReadOnlyList<string> ForPublishOrCancel(Guid eventId)
    {
        var keys = new List<string> { $"event:detail:pub:{eventId}", $"event:detail:{eventId}" };
        for (var page = 1; page <= 3; page++)
        {
            foreach (var size in new[] { 10, 20, 50 })
                keys.Add($"events:list:published:v1:page{page}:size{size}");
        }
        return keys;
    }
}
