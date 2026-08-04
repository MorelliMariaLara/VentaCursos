using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nexa.Web.Services;

public class PaymentService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public PaymentService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public bool IsMercadoPagoConfigured() =>
        !string.IsNullOrWhiteSpace(_config["MP_ACCESS_TOKEN"]) &&
        !string.IsNullOrWhiteSpace(_config["MP_PUBLIC_KEY"]);

    public bool AllowSimulatePayments()
    {
        var flag = _config["MP_ALLOW_SIMULATE"];
        if (string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(flag, "false", StringComparison.OrdinalIgnoreCase)) return false;
        return !IsMercadoPagoConfigured();
    }

    public string? GetPublicKey() => _config["MP_PUBLIC_KEY"];

    public async Task<JsonElement> CreatePreferenceAsync(PreferenceInput input)
    {
        var appUrl = _config["APP_URL"] ?? "http://localhost:5000";
        var body = new
        {
            items = new[]
            {
                new
                {
                    id = input.CourseId,
                    title = input.Title,
                    quantity = 1,
                    unit_price = input.Amount,
                    currency_id = input.Currency,
                },
            },
            payer = new { email = input.PayerEmail },
            external_reference = input.OrderId,
            metadata = new
            {
                order_id = input.OrderId,
                course_id = input.CourseId,
                slug = input.Slug,
            },
            back_urls = new
            {
                success = $"{appUrl}/Checkout?slug={input.Slug}&status=success",
                failure = $"{appUrl}/Checkout?slug={input.Slug}&status=failure",
                pending = $"{appUrl}/Checkout?slug={input.Slug}&status=pending",
            },
            auto_return = "approved",
            notification_url = _config["MP_WEBHOOK_URL"],
        };

        return await MpFetchAsync("/checkout/preferences", HttpMethod.Post, body);
    }

    public Task<JsonElement> CreatePaymentAsync(object formData, string idempotencyKey) =>
        MpFetchAsync("/v1/payments", HttpMethod.Post, formData, idempotencyKey);

    public Task<JsonElement> GetPaymentAsync(string paymentId) =>
        MpFetchAsync($"/v1/payments/{paymentId}", HttpMethod.Get);

    public static string MapMpStatus(string? status) => status switch
    {
        "approved" => "paid",
        "pending" or "authorized" => "pending",
        "in_process" or "in_mediation" => "in_process",
        "rejected" => "rejected",
        "cancelled" => "cancelled",
        "refunded" or "charged_back" => "refunded",
        _ => "failed",
    };

    private async Task<JsonElement> MpFetchAsync(string path, HttpMethod method, object? body = null, string? idempotencyKey = null)
    {
        var token = _config["MP_ACCESS_TOKEN"] ?? throw new InvalidOperationException("MP_ACCESS_TOKEN_MISSING");
        var client = _httpClientFactory.CreateClient();
        using var req = new HttpRequestMessage(method, $"https://api.mercadopago.com{path}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (!string.IsNullOrEmpty(idempotencyKey))
            req.Headers.TryAddWithoutValidation("X-Idempotency-Key", idempotencyKey);
        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var res = await client.SendAsync(req);
        var text = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        var clone = doc.RootElement.Clone();
        if (!res.IsSuccessStatusCode)
        {
            var msg = clone.TryGetProperty("message", out var m) ? m.GetString() : "MP_API_ERROR";
            throw new InvalidOperationException(msg);
        }
        return clone;
    }
}

public record PreferenceInput(
    string OrderId,
    string Title,
    decimal Amount,
    string Currency,
    string PayerEmail,
    string CourseId,
    string Slug);
