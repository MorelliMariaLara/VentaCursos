using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nexa.Web.Services;

public class PaymentService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public PaymentService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public bool IsMercadoPagoConfigured() =>
        !string.IsNullOrWhiteSpace(_config["MP_ACCESS_TOKEN"]) &&
        !string.IsNullOrWhiteSpace(_config["MP_PUBLIC_KEY"]) &&
        !_config["MP_PUBLIC_KEY"]!.Contains("xxxxxxxx", StringComparison.OrdinalIgnoreCase) &&
        !_config["MP_ACCESS_TOKEN"]!.Contains("xxxxxxxx", StringComparison.OrdinalIgnoreCase);

    public bool AllowSimulatePayments()
    {
        var flag = _config["MP_ALLOW_SIMULATE"];
        if (string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(flag, "false", StringComparison.OrdinalIgnoreCase)) return false;
        return !IsMercadoPagoConfigured();
    }

    public string? GetPublicKey() => _config["MP_PUBLIC_KEY"];

    public string AppUrl => (_config["APP_URL"] ?? "http://localhost:5000").TrimEnd('/');

    public string WebhookUrl =>
        !string.IsNullOrWhiteSpace(_config["MP_WEBHOOK_URL"])
            ? _config["MP_WEBHOOK_URL"]!
            : $"{AppUrl}/api/webhooks/mercadopago";

    public async Task<JsonElement> CreatePreferenceAsync(PreferenceInput input)
    {
        var body = new
        {
            items = new[]
            {
                new
                {
                    id = input.CourseId,
                    title = input.Title,
                    description = $"Curso SANTICAZA: {input.Title}",
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
                success = $"{AppUrl}/Checkout?slug={Uri.EscapeDataString(input.Slug)}&status=success",
                failure = $"{AppUrl}/Checkout?slug={Uri.EscapeDataString(input.Slug)}&status=failure",
                pending = $"{AppUrl}/Checkout?slug={Uri.EscapeDataString(input.Slug)}&status=pending",
            },
            auto_return = "approved",
            notification_url = WebhookUrl,
            statement_descriptor = "SANTICAZA",
        };

        return await MpFetchAsync("/checkout/preferences", HttpMethod.Post, body);
    }

    /// <summary>
    /// Crea el pago a partir del formData del Payment Brick,
    /// completando monto, referencia y webhook.
    /// </summary>
    public async Task<JsonElement> CreatePaymentFromBrickAsync(
        JsonElement? formData,
        PreferenceInput orderInfo,
        string idempotencyKey)
    {
        JsonObject payload;
        if (formData.HasValue && formData.Value.ValueKind is JsonValueKind.Object)
        {
            payload = JsonNode.Parse(formData.Value.GetRawText())!.AsObject();
        }
        else
        {
            payload = new JsonObject();
        }

        // Campos obligatorios / de negocio (no pisar si Brick ya los mandó bien)
        if (!payload.ContainsKey("transaction_amount") || payload["transaction_amount"] is null)
            payload["transaction_amount"] = orderInfo.Amount;

        payload["description"] = $"SANTICAZA · {orderInfo.Title}";
        payload["external_reference"] = orderInfo.OrderId;
        payload["notification_url"] = WebhookUrl;

        if (payload["payer"] is not JsonObject)
        {
            payload["payer"] = new JsonObject { ["email"] = orderInfo.PayerEmail };
        }
        else if (payload["payer"] is JsonObject payer &&
                 (payer["email"] is null || string.IsNullOrWhiteSpace(payer["email"]?.ToString())))
        {
            payer["email"] = orderInfo.PayerEmail;
        }

        if (!payload.ContainsKey("metadata") || payload["metadata"] is null)
        {
            payload["metadata"] = new JsonObject
            {
                ["order_id"] = orderInfo.OrderId,
                ["course_id"] = orderInfo.CourseId,
                ["slug"] = orderInfo.Slug,
            };
        }

        return await MpFetchAsync("/v1/payments", HttpMethod.Post, payload, idempotencyKey);
    }

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

    public static bool IsAccredited(string mappedStatus) => mappedStatus == "paid";

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
            string json = body switch
            {
                JsonObject node => node.ToJsonString(),
                JsonElement el => el.GetRawText(),
                _ => JsonSerializer.Serialize(body),
            };
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var res = await client.SendAsync(req);
        var text = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        var clone = doc.RootElement.Clone();
        if (!res.IsSuccessStatusCode)
        {
            var msg = clone.TryGetProperty("message", out var m)
                ? m.GetString()
                : clone.TryGetProperty("error", out var e)
                    ? e.GetString()
                    : "MP_API_ERROR";
            var cause = "";
            if (clone.TryGetProperty("cause", out var causes) && causes.ValueKind == JsonValueKind.Array)
            {
                cause = string.Join("; ", causes.EnumerateArray()
                    .Select(c => c.TryGetProperty("description", out var d) ? d.GetString() : null)
                    .Where(x => !string.IsNullOrEmpty(x)));
            }
            throw new InvalidOperationException(string.IsNullOrEmpty(cause) ? (msg ?? "MP_API_ERROR") : $"{msg}: {cause}");
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
