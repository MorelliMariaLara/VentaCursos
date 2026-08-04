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

    private string? AccessToken => _config["MP_ACCESS_TOKEN"]?.Trim();
    private string? PublicKey => _config["MP_PUBLIC_KEY"]?.Trim();

    public bool IsMercadoPagoConfigured() =>
        !string.IsNullOrWhiteSpace(AccessToken) &&
        !string.IsNullOrWhiteSpace(PublicKey) &&
        !PublicKey!.Contains("xxxxxxxx", StringComparison.OrdinalIgnoreCase) &&
        !AccessToken!.Contains("xxxxxxxx", StringComparison.OrdinalIgnoreCase);

    public bool AllowSimulatePayments()
    {
        var flag = _config["MP_ALLOW_SIMULATE"];
        if (string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(flag, "false", StringComparison.OrdinalIgnoreCase)) return false;
        return !IsMercadoPagoConfigured();
    }

    public string? GetPublicKey() => PublicKey;

    public string AppUrl => (_config["APP_URL"] ?? "http://localhost:5000").TrimEnd('/');

    public bool IsPublicHttpsApp =>
        AppUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
        !AppUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase);

    public string? WebhookUrl
    {
        get
        {
            var configured = _config["MP_WEBHOOK_URL"]?.Trim();
            if (!string.IsNullOrWhiteSpace(configured) &&
                configured.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !configured.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                return configured;
            }

            // MP rechaza webhooks a localhost → no enviarlos en local
            if (!IsPublicHttpsApp) return null;
            return $"{AppUrl}/api/webhooks/mercadopago";
        }
    }

    public async Task<JsonElement> CreatePreferenceAsync(PreferenceInput input)
    {
        // Body dinámico: en localhost NO mandamos notification_url ni auto_return
        // (con credenciales APP_USR eso dispara "At least one policy returned UNAUTHORIZED")
        var root = new JsonObject
        {
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = input.CourseId,
                    ["title"] = input.Title,
                    ["description"] = $"Curso SANTICAZA: {input.Title}",
                    ["quantity"] = 1,
                    // double evita rarezas de serialización decimal en JsonObject
                    ["unit_price"] = (double)input.Amount,
                    ["currency_id"] = input.Currency,
                },
            },
            ["payer"] = new JsonObject { ["email"] = input.PayerEmail },
            ["external_reference"] = input.OrderId,
            ["metadata"] = new JsonObject
            {
                ["order_id"] = input.OrderId,
                ["course_id"] = input.CourseId,
                ["slug"] = input.Slug,
            },
        };

        if (IsPublicHttpsApp)
        {
            root["back_urls"] = new JsonObject
            {
                ["success"] = $"{AppUrl}/Checkout?slug={Uri.EscapeDataString(input.Slug)}&status=success",
                ["failure"] = $"{AppUrl}/Checkout?slug={Uri.EscapeDataString(input.Slug)}&status=failure",
                ["pending"] = $"{AppUrl}/Checkout?slug={Uri.EscapeDataString(input.Slug)}&status=pending",
            };
            root["auto_return"] = "approved";
        }

        var hook = WebhookUrl;
        if (!string.IsNullOrEmpty(hook))
            root["notification_url"] = hook;

        return await MpFetchAsync("/checkout/preferences", HttpMethod.Post, root);
    }

    public async Task<JsonElement> CreatePaymentFromBrickAsync(
        JsonElement? formData,
        PreferenceInput orderInfo,
        string idempotencyKey)
    {
        JsonObject payload;
        if (formData.HasValue && formData.Value.ValueKind is JsonValueKind.Object)
            payload = JsonNode.Parse(formData.Value.GetRawText())!.AsObject();
        else
            payload = new JsonObject();

        if (!payload.ContainsKey("transaction_amount") || payload["transaction_amount"] is null)
            payload["transaction_amount"] = orderInfo.Amount;

        // MP espera número (no string) en transaction_amount
        if (payload["transaction_amount"] is JsonValue amtVal && amtVal.TryGetValue<string>(out var amtStr) &&
            decimal.TryParse(amtStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsedAmt))
        {
            payload["transaction_amount"] = parsedAmt;
        }

        payload["description"] = $"SANTICAZA · {orderInfo.Title}";
        payload["external_reference"] = orderInfo.OrderId;

        var hook = WebhookUrl;
        if (!string.IsNullOrEmpty(hook))
            payload["notification_url"] = hook;
        else
            payload.Remove("notification_url");

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

        // Idempotency única por intento (reusar order.Id falla si se reintenta el Brick)
        var key = $"{idempotencyKey}-{Guid.NewGuid():N}"[..64];
        return await MpFetchAsync("/v1/payments", HttpMethod.Post, payload, key);
    }

    public Task<JsonElement> GetPaymentAsync(string paymentId) =>
        MpFetchAsync($"/v1/payments/{paymentId}", HttpMethod.Get);

    /// <summary>
    /// Busca pagos de una orden (útil en local sin webhook, tras Wallet Brick / Checkout Pro).
    /// </summary>
    public async Task<JsonElement?> FindLatestPaymentByExternalReferenceAsync(string externalReference)
    {
        var path =
            "/v1/payments/search?sort=date_created&criteria=desc&external_reference=" +
            Uri.EscapeDataString(externalReference);
        var result = await MpFetchAsync(path, HttpMethod.Get);
        if (!result.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var item in results.EnumerateArray())
            return item.Clone();
        return null;
    }

    public string CredentialDiagnostics()
    {
        var pk = PublicKey ?? "";
        var tk = AccessToken ?? "";
        static string Mask(string v) =>
            v.Length <= 12 ? "(corto)" : $"{v[..10]}…{v[^6..]} (len={v.Length})";
        return $"MP configurado={IsMercadoPagoConfigured()} PK={Mask(pk)} TK={Mask(tk)} AppUrl={AppUrl} Webhook={(WebhookUrl ?? "(local: omitido)")}";
    }

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

    public static string FriendlyError(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Error de Mercado Pago";
        if (raw.Contains("UNAUTHORIZED", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("pa_unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return "Mercado Pago rechazó la autorización. Verificá que MP_PUBLIC_KEY y MP_ACCESS_TOKEN sean el par de la misma aplicación (Pruebas), sin espacios, y reiniciá la app. En local no uses notification_url a localhost.";
        }
        if (raw.Contains("invalid_token", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("Invalid access token", StringComparison.OrdinalIgnoreCase))
        {
            return "Access Token inválido. Copiá de nuevo el Access Token de prueba desde Tus integraciones.";
        }
        return raw.Length > 220 ? raw[..220] + "…" : raw;
    }

    private async Task<JsonElement> MpFetchAsync(string path, HttpMethod method, object? body = null, string? idempotencyKey = null)
    {
        var token = AccessToken ?? throw new InvalidOperationException("MP_ACCESS_TOKEN_MISSING");
        if (token.Contains(' ') || token.Contains('\n') || token.Contains('\r'))
            throw new InvalidOperationException("MP_ACCESS_TOKEN tiene espacios o saltos de línea. Pegalo en una sola línea.");

        var client = _httpClientFactory.CreateClient();
        using var req = new HttpRequestMessage(method, $"https://api.mercadopago.com{path}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
                    : $"MP_API_ERROR_{(int)res.StatusCode}";
            var cause = "";
            if (clone.TryGetProperty("cause", out var causes) && causes.ValueKind == JsonValueKind.Array)
            {
                cause = string.Join("; ", causes.EnumerateArray()
                    .Select(c =>
                    {
                        var d = c.TryGetProperty("description", out var desc) ? desc.GetString() : null;
                        var cde = c.TryGetProperty("code", out var code) ? code.ToString() : null;
                        return string.Join(" ", new[] { cde, d }.Where(x => !string.IsNullOrEmpty(x)));
                    })
                    .Where(x => !string.IsNullOrEmpty(x)));
            }
            var raw = string.IsNullOrEmpty(cause) ? (msg ?? "MP_API_ERROR") : $"{msg}: {cause}";
            Console.Error.WriteLine($"[MP] {(int)res.StatusCode} {path} → {raw}");
            throw new InvalidOperationException(FriendlyError(raw));
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
