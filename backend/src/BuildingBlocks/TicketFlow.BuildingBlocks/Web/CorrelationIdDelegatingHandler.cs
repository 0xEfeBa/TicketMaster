using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace TicketFlow.BuildingBlocks.Web;

public class CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            if (!request.Headers.Contains(CorrelationIdHeader))
            {
                request.Headers.Add(CorrelationIdHeader, correlationId.ToString());
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
