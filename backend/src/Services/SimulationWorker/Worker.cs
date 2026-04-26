using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TicketFlow.SimulationWorker;

public class Worker(
    ILogger<Worker> logger,
    IHttpClientFactory httpClientFactory,
    IOptions<SimulationOptions> options) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly SimulationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("=================================================");
        logger.LogInformation("TICKETFLOW SIMULATION WORKER STARTED");
        logger.LogInformation("Config => MaxCycles={MaxCycles}, NegRepeat={NegRepeat}, LoadUsers={LoadUsers}, LoadBatch={LoadBatch}, Parallelism={Parallelism}",
            _options.MaxCycles, _options.NegativeScenarioRepeatCount, _options.LoadUsers, _options.LoadBatchSize, _options.LoadParallelism);
        logger.LogInformation("=================================================");

        await Task.Delay(TimeSpan.FromSeconds(_options.InitialDelaySeconds), stoppingToken);

        var cycle = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            cycle++;
            var report = new CycleReport(cycle);

            try
            {
                logger.LogInformation("---- Simulation cycle #{Cycle} started ----", cycle);
                await RunCycleAsync(report, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cycle #{Cycle} unexpected failure.", cycle);
            }
            finally
            {
                PrintCycleSummary(report);
            }

            if (_options.MaxCycles > 0 && cycle >= _options.MaxCycles)
            {
                logger.LogInformation("MaxCycles reached ({MaxCycles}). Worker exits.", _options.MaxCycles);
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.CycleDelaySeconds), stoppingToken);
        }
    }

    private async Task RunCycleAsync(CycleReport report, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("GatewayClient");
        var negativeRepeatCount = Math.Max(1, _options.NegativeScenarioRepeatCount);

        await CheckHealthAsync(client, report, ct);

        var adminToken = await LoginAsync(client, _options.AdminEmail, _options.AdminPassword, report, "admin-login", ct);
        if (string.IsNullOrWhiteSpace(adminToken))
        {
            logger.LogWarning("Admin login failed. Cycle stopped.");
            return;
        }

        var target = await ResolveBookableEventAsync(client, report, ct);
        if (target is null)
        {
            logger.LogWarning("No bookable published event found. Cycle stopped.");
            return;
        }
        var targetEventId = target.Value.EventId;
        var targetTicketTypeId = target.Value.TicketTypeId;

        if (_options.EnableAdminScenario)
            await RunAdminCatalogFlowAsync(client, adminToken, targetEventId, report, ct);

        var organizer = await RegisterAndLoginAsync(client, "organizer", report, ct);
        if (organizer is not null)
            await RunOrganizerFlowAsync(client, organizer, adminToken, report, ct, negativeRepeatCount);

        var customer = await RegisterAndLoginAsync(client, "customer", report, ct);
        if (customer is null)
        {
            logger.LogWarning("Customer flow could not start.");
            return;
        }

        await RunIdentityFlowAsync(client, customer, report, ct);

        if (_options.EnableBookingScenario)
            await RunBookingFlowAsync(client, customer.AccessToken, targetEventId, targetTicketTypeId, report, ct);

        if (_options.EnableRaceScenario)
        {
            for (var i = 1; i <= negativeRepeatCount; i++)
            {
                await RunConfirmRaceAsync(client, customer.AccessToken, targetEventId, targetTicketTypeId, report, ct);
                logger.LogInformation("Race repeat completed => {Current}/{Total}", i, negativeRepeatCount);
            }
        }

        if (_options.EnableLoadScenario)
            await RunLoadTestAsync(client, targetEventId, targetTicketTypeId, report, ct);
    }

    private async Task CheckHealthAsync(HttpClient client, CycleReport report, CancellationToken ct)
    {
        await SendAsync(client, HttpMethod.Get, "/health", null, null, null, "gateway-health", report, ct);
        await SendAsync(client, HttpMethod.Get, "/health/details", null, null, null, "gateway-health-details", report, ct);
    }

    private async Task RunAdminCatalogFlowAsync(HttpClient client, string adminToken, Guid eventId, CycleReport report, CancellationToken ct)
    {
        var addTicket = await SendAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/events/{eventId}/ticket-types",
            new { name = $"VIP-{Guid.NewGuid().ToString("N")[..6]}", priceAmount = 399.99m, totalQuantity = 250 },
            adminToken,
            null,
            "catalog-add-tickettype-admin",
            report,
            ct);

        if (addTicket.IsSuccessStatusCode)
            logger.LogInformation("Admin ticket type add succeeded on event {EventId}.", eventId);
    }

    private async Task RunOrganizerFlowAsync(
        HttpClient client,
        TokenBundle organizer,
        string adminToken,
        CycleReport report,
        CancellationToken ct,
        int negativeRepeatCount)
    {
        var me = await GetMeAsync(client, organizer.AccessToken, "identity-me-organizer", report, ct);
        if (me is null)
            return;

        var assign = await SendAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/admin/users/{me.Id}/role",
            new { role = "Organizer" },
            adminToken,
            null,
            "identity-assign-role-organizer",
            report,
            ct);
        if (!assign.IsSuccessStatusCode)
            return;

        var organizerToken = await LoginAsync(client, organizer.Email, organizer.Password, report, "organizer-relogin", ct);
        if (string.IsNullOrWhiteSpace(organizerToken))
            return;

        for (var i = 1; i <= negativeRepeatCount; i++)
        {
            var createRes = await SendAsync(
                client,
                HttpMethod.Post,
                "/api/v1/events",
                new
                {
                    title = $"Sim Event {Guid.NewGuid().ToString("N")[..6]}",
                    description = "Simulation created event",
                    venue = "Demo Arena",
                    imageUrl = "https://picsum.photos/seed/sim/800/400"
                },
                organizerToken,
                null,
                "catalog-create-organizer",
                report,
                ct);

            if (createRes.StatusCode != HttpStatusCode.Created)
                return;

            var createPayload = await ReadJsonSafeAsync<CreateEventResponse>(createRes, ct);
            if (createPayload?.Id is not Guid createdEventId || createdEventId == Guid.Empty)
                return;

            await SendAsync(
                client,
                HttpMethod.Post,
                $"/api/v1/events/{createdEventId}/ticket-types",
                new { name = "Standard", priceAmount = 120m, totalQuantity = 1000 },
                organizerToken,
                null,
                "catalog-add-tickettype-organizer",
                report,
                ct);

            var publishRes = await SendAsync(
                client,
                HttpMethod.Post,
                $"/api/v1/events/{createdEventId}/publish",
                null,
                organizerToken,
                null,
                "catalog-publish-organizer-no-session",
                report,
                ct);
            if ((int)publishRes.StatusCode >= 500)
                logger.LogWarning("Organizer publish returned unexpected 5xx: {Status}", (int)publishRes.StatusCode);

            var cancelCreatedByAdmin = await SendAsync(
                client,
                HttpMethod.Delete,
                $"/api/v1/events/{createdEventId}",
                null,
                adminToken,
                null,
                "catalog-cancel-admin",
                report,
                ct);
            var anonymousAfterCancel = await SendAsync(
                client,
                HttpMethod.Get,
                $"/api/v1/events/{createdEventId}",
                null,
                null,
                null,
                "catalog-detail-anon-after-cancel",
                report,
                ct);

            logger.LogInformation(
                "Organizer negative repeat {Current}/{Total} => publishNoSession={PublishStatus}, cancel={CancelStatus}, anonAfterCancel={AnonCancel}",
                i, negativeRepeatCount, (int)publishRes.StatusCode, (int)cancelCreatedByAdmin.StatusCode, (int)anonymousAfterCancel.StatusCode);
        }
    }

    private async Task<(Guid EventId, Guid TicketTypeId)?> ResolveBookableEventAsync(
        HttpClient client,
        CycleReport report,
        CancellationToken ct)
    {
        var events = await GetEventsAsync(client, report, ct);
        if (events.Count == 0)
            return null;

        foreach (var e in events)
        {
            var detail = await GetEventDetailAsync(client, e.Id, null, report, ct, "catalog-event-detail-candidate");
            var ticketType = detail?.TicketTypes.FirstOrDefault();
            if (ticketType is not null)
                return (e.Id, ticketType.Id);
        }

        return null;
    }

    private async Task RunIdentityFlowAsync(HttpClient client, TokenBundle customer, CycleReport report, CancellationToken ct)
    {
        await GetMeAsync(client, customer.AccessToken, "identity-me-customer", report, ct);

        var refreshRes = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/refresh",
            new { refreshToken = customer.RefreshToken },
            null,
            null,
            "identity-refresh-customer",
            report,
            ct);
        var refreshed = await ReadJsonSafeAsync<AuthResponse>(refreshRes, ct);
        var logoutToken = refreshed?.AccessToken ?? customer.AccessToken;

        await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/logout",
            new { refreshToken = customer.RefreshToken },
            logoutToken,
            null,
            "identity-logout-customer",
            report,
            ct);
    }

    private async Task RunBookingFlowAsync(
        HttpClient client,
        string customerToken,
        Guid eventId,
        Guid ticketTypeId,
        CycleReport report,
        CancellationToken ct)
    {
        var idem = Guid.NewGuid().ToString();
        var body = new { eventId, ticketTypeId, quantity = 1 };

        var holdA = SendAsync(client, HttpMethod.Post, "/api/v1/reservations", body, customerToken, idem, "booking-hold-idempotency-a", report, ct);
        var holdB = SendAsync(client, HttpMethod.Post, "/api/v1/reservations", body, customerToken, idem, "booking-hold-idempotency-b", report, ct);
        await Task.WhenAll(holdA, holdB);

        var responses = new[] { holdA.Result, holdB.Result };
        var winner = responses.FirstOrDefault(r => r.StatusCode == HttpStatusCode.Created) ?? responses.First();
        var holdPayload = await ReadJsonSafeAsync<HoldResponse>(winner, ct);
        if (holdPayload?.ReservationId is not Guid reservationId || reservationId == Guid.Empty)
            return;

        await SendAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/reservations/{reservationId}/confirm",
            null,
            customerToken,
            Guid.NewGuid().ToString(),
            "booking-confirm",
            report,
            ct);
    }

    private async Task RunConfirmRaceAsync(
        HttpClient client,
        string customerToken,
        Guid eventId,
        Guid ticketTypeId,
        CycleReport report,
        CancellationToken ct)
    {
        var holdRes = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/reservations",
            new { eventId, ticketTypeId, quantity = 1 },
            customerToken,
            Guid.NewGuid().ToString(),
            "booking-race-hold",
            report,
            ct);
        var holdPayload = await ReadJsonSafeAsync<HoldResponse>(holdRes, ct);
        if (holdPayload?.ReservationId is not Guid reservationId || reservationId == Guid.Empty)
            return;

        var c1 = SendAsync(client, HttpMethod.Post, $"/api/v1/reservations/{reservationId}/confirm", null, customerToken, Guid.NewGuid().ToString(), "booking-race-confirm-a", report, ct);
        var c2 = SendAsync(client, HttpMethod.Post, $"/api/v1/reservations/{reservationId}/confirm", null, customerToken, Guid.NewGuid().ToString(), "booking-race-confirm-b", report, ct);
        await Task.WhenAll(c1, c2);

        logger.LogInformation("Confirm race statuses => {A}, {B}", (int)c1.Result.StatusCode, (int)c2.Result.StatusCode);
    }

    private async Task RunLoadTestAsync(
        HttpClient baseClient,
        Guid eventId,
        Guid ticketTypeId,
        CycleReport report,
        CancellationToken ct)
    {
        var users = _options.LoadUsers;
        var batchSize = Math.Max(1, _options.LoadBatchSize);
        var parallelism = Math.Max(1, _options.LoadParallelism);
        var pauseSeconds = Math.Max(0, _options.LoadBatchPauseSeconds);

        logger.LogInformation("Load test started => users={Users}, batch={Batch}, parallelism={Parallelism}", users, batchSize, parallelism);

        var limiter = new SemaphoreSlim(parallelism, parallelism);
        var tasks = new List<Task>();

        for (var i = 0; i < users; i++)
        {
            await limiter.WaitAsync(ct);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("GatewayClient");
                    var user = await RegisterAndLoginForLoadAsync(client, report, ct);
                    if (user is null)
                        return;

                    await SendAsync(
                        client,
                        HttpMethod.Post,
                        "/api/v1/reservations",
                        new { eventId, ticketTypeId, quantity = 1 },
                        user.AccessToken,
                        Guid.NewGuid().ToString(),
                        "booking-load-hold",
                        report,
                        ct);
                }
                finally
                {
                    limiter.Release();
                }
            }, ct));

            if ((i + 1) % batchSize == 0)
            {
                await Task.WhenAll(tasks);
                tasks.Clear();
                logger.LogInformation("Load batch completed => {Completed}/{Total}", i + 1, users);
                if (pauseSeconds > 0)
                    await Task.Delay(TimeSpan.FromSeconds(pauseSeconds), ct);
            }
        }

        if (tasks.Count > 0)
            await Task.WhenAll(tasks);

        logger.LogInformation("Load test completed => users={Users}", users);
    }

    private async Task<TokenBundle?> RegisterAndLoginForLoadAsync(HttpClient client, CycleReport report, CancellationToken ct)
    {
        var retries = Math.Max(1, _options.LoadRegisterRetryCount);
        for (var attempt = 1; attempt <= retries; attempt++)
        {
            var user = await RegisterAndLoginAsync(client, "load", report, ct);
            if (user is not null)
                return user;

            if (attempt < retries)
                await Task.Delay(TimeSpan.FromMilliseconds(600 * attempt), ct);
        }

        return null;
    }

    private async Task<TokenBundle?> RegisterAndLoginAsync(HttpClient client, string prefix, CycleReport report, CancellationToken ct)
    {
        var email = $"{prefix}_{Guid.NewGuid().ToString("N")[..8]}@demo.local";
        var password = _options.DefaultPassword;

        var register = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/register",
            new { email, password },
            null,
            null,
            $"identity-register-{prefix}",
            report,
            ct);
        if (!register.IsSuccessStatusCode)
            return null;

        var auth = await ReadJsonSafeAsync<AuthResponse>(register, ct);
        if (auth is null)
            return null;

        return new TokenBundle(email, password, auth.AccessToken, auth.RefreshToken);
    }

    private async Task<string?> LoginAsync(
        HttpClient client,
        string email,
        string password,
        CycleReport report,
        string metricName,
        CancellationToken ct)
    {
        var loginRes = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/login",
            new { email, password },
            null,
            null,
            metricName,
            report,
            ct);
        if (!loginRes.IsSuccessStatusCode)
            return null;
        var token = await ReadJsonSafeAsync<AuthResponse>(loginRes, ct);
        return token?.AccessToken;
    }

    private async Task<UserMeResponse?> GetMeAsync(HttpClient client, string token, string metricName, CycleReport report, CancellationToken ct)
    {
        var meRes = await SendAsync(client, HttpMethod.Get, "/api/v1/users/me", null, token, null, metricName, report, ct);
        if (!meRes.IsSuccessStatusCode)
            return null;
        return await ReadJsonSafeAsync<UserMeResponse>(meRes, ct);
    }

    private async Task<List<EventListItem>> GetEventsAsync(HttpClient client, CycleReport report, CancellationToken ct)
    {
        var res = await SendAsync(client, HttpMethod.Get, "/api/v1/events?page=1&pageSize=10", null, null, null, "catalog-events-list", report, ct);
        var events = await ReadJsonSafeAsync<List<EventListItem>>(res, ct);
        return events ?? [];
    }

    private async Task<EventDetailResponse?> GetEventDetailAsync(
        HttpClient client,
        Guid eventId,
        string? token,
        CycleReport report,
        CancellationToken ct,
        string metricName = "catalog-event-detail")
    {
        var res = await SendAsync(client, HttpMethod.Get, $"/api/v1/events/{eventId}", null, token, null, metricName, report, ct);
        if (!res.IsSuccessStatusCode)
            return null;
        return await ReadJsonSafeAsync<EventDetailResponse>(res, ct);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object? body,
        string? bearerToken,
        string? idempotencyKey,
        string metricName,
        CycleReport report,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, path);
        if (body is not null)
            req.Content = JsonContent.Create(body, options: JsonOptions);
        if (!string.IsNullOrWhiteSpace(bearerToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            req.Headers.Add("Idempotency-Key", idempotencyKey);

        var sw = Stopwatch.StartNew();
        var res = await client.SendAsync(req, ct);
        sw.Stop();

        var bodySnippet = string.Empty;
        if (!res.IsSuccessStatusCode)
        {
            var raw = await res.Content.ReadAsStringAsync(ct);
            bodySnippet = raw.Length > 240 ? raw[..240] : raw;
        }

        report.Track(metricName, res.StatusCode, sw.ElapsedMilliseconds);

        if (!res.IsSuccessStatusCode)
            logger.LogWarning("[{Metric}] {Method} {Path} => {Status} body={Body}", metricName, method.Method, path, (int)res.StatusCode, bodySnippet);

        return res;
    }

    private static async Task<T?> ReadJsonSafeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        }
        catch
        {
            return default;
        }
    }

    private void PrintCycleSummary(CycleReport report)
    {
        var rows = report.Snapshot();
        var totalSuccess = rows.Sum(r => r.Success);
        var totalFail = rows.Sum(r => r.Fail);
        var totalRequests = totalSuccess + totalFail;

                logger.LogInformation("=================================================");
        logger.LogInformation("SIMULATION CYCLE #{Cycle} SUMMARY", report.CycleNo);
        logger.LogInformation("TotalRequests={TotalRequests} Success={Success} Fail={Fail}", totalRequests, totalSuccess, totalFail);

        foreach (var row in rows.OrderBy(r => r.Endpoint))
        {
            var p95 = Percentile(row.DurationsMs, 0.95);
            var avg = row.DurationsMs.Count == 0 ? 0 : row.DurationsMs.Average();
            var statuses = string.Join(",", row.Statuses.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Value}"));

            logger.LogInformation(
                "Endpoint={Endpoint} ok={Ok} fail={Fail} avgMs={Avg:F1} p95Ms={P95} statuses=[{Statuses}]",
                row.Endpoint, row.Success, row.Fail, avg, p95, statuses);
        }
                logger.LogInformation("=================================================");
    }

    private static long Percentile(List<long> values, double percentile)
    {
        if (values.Count == 0)
            return 0;
        values.Sort();
        var idx = (int)Math.Ceiling(values.Count * percentile) - 1;
        idx = Math.Clamp(idx, 0, values.Count - 1);
        return values[idx];
    }

    private sealed class CycleReport(int cycleNo)
    {
        private readonly ConcurrentDictionary<string, EndpointCounter> _endpoints = new();
        public int CycleNo { get; } = cycleNo;

        public void Track(string endpoint, HttpStatusCode status, long durationMs)
        {
            var item = _endpoints.GetOrAdd(endpoint, _ => new EndpointCounter());
            item.Track((int)status, durationMs);
        }

        public List<EndpointSnapshot> Snapshot() =>
            _endpoints.Select(kvp => kvp.Value.Snapshot(kvp.Key)).ToList();
    }

    private sealed class EndpointCounter
    {
        private readonly object _gate = new();
        private int _success;
        private int _fail;
        private readonly List<long> _durations = [];
        private readonly Dictionary<int, int> _statuses = [];

        public void Track(int statusCode, long durationMs)
        {
            lock (_gate)
            {
                if (statusCode is >= 200 and < 300)
                    _success++;
                else
                    _fail++;

                _durations.Add(durationMs);
                _statuses[statusCode] = _statuses.TryGetValue(statusCode, out var n) ? n + 1 : 1;
            }
        }

        public EndpointSnapshot Snapshot(string endpoint)
        {
            lock (_gate)
            {
                return new EndpointSnapshot(endpoint, _success, _fail, [.. _durations], new Dictionary<int, int>(_statuses));
            }
        }
    }

    private record EndpointSnapshot(
        string Endpoint,
        int Success,
        int Fail,
        List<long> DurationsMs,
        Dictionary<int, int> Statuses);

    private record AuthResponse(string AccessToken, int ExpiresIn, string TokenType, string RefreshToken, int RefreshExpiresIn);
    private record TokenBundle(string Email, string Password, string AccessToken, string RefreshToken);
    private record UserMeResponse(Guid Id, string Email, string Role);
    private record EventListItem(Guid Id, string Title);
    private record EventDetailResponse(Guid Id, List<TicketTypeItem> TicketTypes);
    private record TicketTypeItem(Guid Id, string Name);
    private record HoldResponse(Guid ReservationId);
    private record CreateEventResponse(Guid Id);
}
