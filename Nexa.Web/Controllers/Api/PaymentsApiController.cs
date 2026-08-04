using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers.Api;

[ApiController]
[Route("api")]
public class PaymentsApiController : ControllerBase
{
    private readonly StoreService _store;
    private readonly PaymentService _payments;

    public PaymentsApiController(StoreService store, PaymentService payments)
    {
        _store = store;
        _payments = payments;
    }

    [HttpGet("payments/config")]
    public IActionResult Config() => Ok(new
    {
        configured = _payments.IsMercadoPagoConfigured(),
        simulate = _payments.AllowSimulatePayments(),
        publicKey = _payments.GetPublicKey(),
        testCredentials = _payments.IsTestCredentials(),
        pairOk = _payments.CredentialsPairLooksConsistent(),
        diagnostics = _payments.CredentialDiagnostics(),
        webhookUrl = _payments.IsMercadoPagoConfigured() ? _payments.WebhookUrl : null,
    });

    public record PrefBody(string? Slug);
    public record ProcessBody(string? OrderId, bool Simulate, JsonElement? FormData, string? SelectedPaymentMethod);

    [Authorize]
    [HttpPost("payments/preference")]
    public async Task<IActionResult> Preference([FromBody] PrefBody body)
    {
        var userId = AuthCookie.UserId(User)!;
        var course = await _store.GetCourseBySlugAsync(body.Slug ?? "");
        if (course == null) return NotFound(new { error = "Curso no encontrado" });
        if (await _store.GetEnrollmentAsync(userId, course.Id) != null)
            return Conflict(new { error = "Ya tenés este curso", slug = course.Slug });

        try
        {
            var order = await _store.CreatePendingOrderAsync(userId, course.Id, course.Price, course.Currency);
            if (!_payments.IsMercadoPagoConfigured())
            {
                return Ok(new
                {
                    orderId = order.Id,
                    simulateOnly = true,
                    preferenceId = (string?)null,
                    amount = course.Price,
                    currency = course.Currency,
                });
            }

            var pref = await _payments.CreatePreferenceAsync(new PreferenceInput(
                order.Id, course.Title, course.Price, course.Currency,
                AuthCookie.Email(User)!, course.Id, course.Slug));
            var prefId = pref.GetProperty("id").ToString();
            await _store.UpdateOrderAsync(order.Id, o => o.PreferenceId = prefId);

            string? initPoint = pref.TryGetProperty("init_point", out var ip) ? ip.GetString() : null;
            string? sandboxInit = pref.TryGetProperty("sandbox_init_point", out var sip) ? sip.GetString() : null;
            // TEST- → sandbox_init_point; producción/APP_USR → init_point
            var checkoutUrl = _payments.IsTestCredentials()
                ? (sandboxInit ?? initPoint)
                : (initPoint ?? sandboxInit);

            return Ok(new
            {
                orderId = order.Id,
                preferenceId = prefId,
                initPoint,
                sandboxInitPoint = sandboxInit,
                checkoutUrl,
                simulateOnly = false,
                amount = course.Price,
                currency = course.Currency,
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "ALREADY_OWNED")
        {
            return Conflict(new { error = "Ya comprado" });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            Console.Error.WriteLine(_payments.CredentialDiagnostics());
            return StatusCode(500, new
            {
                error = PaymentService.FriendlyError(ex.Message),
                diagnostics = _payments.CredentialDiagnostics(),
                pairOk = _payments.CredentialsPairLooksConsistent(),
            });
        }
    }

    [Authorize]
    [HttpPost("payments/process")]
    public async Task<IActionResult> Process([FromBody] ProcessBody body)
    {
        var userId = AuthCookie.UserId(User)!;
        var order = await _store.GetOrderByIdAsync(body.OrderId ?? "");
        if (order == null || order.UserId != userId) return NotFound(new { error = "Orden no encontrada" });
        var course = await _store.GetCourseByIdAsync(order.CourseId);
        if (course == null) return NotFound(new { error = "Curso no encontrado" });

        // Ya acreditada → habilitar aula
        if (order.Status == "paid")
        {
            if (await _store.GetEnrollmentAsync(userId, course.Id) == null)
                await _store.FulfillPaidOrderAsync(order.Id);
            return Ok(new
            {
                status = "paid",
                accredited = true,
                slug = course.Slug,
                paymentId = order.PaymentId,
                redirect = $"/Learn?slug={course.Slug}",
            });
        }

        try
        {
            if (body.Simulate)
            {
                if (!_payments.AllowSimulatePayments())
                    return BadRequest(new { error = "Simulación deshabilitada. Configurá Mercado Pago." });
                await _store.UpdateOrderAsync(order.Id, o =>
                {
                    o.Simulated = true;
                    o.PaymentMethod = "simulate";
                });
                await _store.FulfillPaidOrderAsync(order.Id);
                return Ok(new
                {
                    status = "paid",
                    accredited = true,
                    slug = course.Slug,
                    paymentId = (string?)null,
                    redirect = $"/Learn?slug={course.Slug}",
                });
            }

            if (!_payments.IsMercadoPagoConfigured())
                return BadRequest(new { error = "Mercado Pago no configurado (MP_PUBLIC_KEY / MP_ACCESS_TOKEN)" });

            var payment = await _payments.CreatePaymentFromBrickAsync(
                body.FormData,
                new PreferenceInput(
                    order.Id, course.Title, course.Price, course.Currency,
                    AuthCookie.Email(User)!, course.Id, course.Slug),
                order.Id);

            var mpStatus = payment.TryGetProperty("status", out var st) ? st.GetString() : null;
            var status = PaymentService.MapMpStatus(mpStatus);
            var paymentId = payment.TryGetProperty("id", out var id) ? id.ToString() : null;

            await _store.UpdateOrderAsync(order.Id, o =>
            {
                o.Status = status;
                o.PaymentId = paymentId;
                o.PaymentMethod = payment.TryGetProperty("payment_method_id", out var pm) ? pm.GetString() : body.SelectedPaymentMethod;
                o.StatusDetail = payment.TryGetProperty("status_detail", out var sd) ? sd.GetString() : null;
                if (payment.TryGetProperty("payer", out var payer) &&
                    payer.TryGetProperty("email", out var email))
                    o.PayerEmail = email.GetString();
            });

            // Solo habilita lecciones/video si el pago está ACREDITADO (approved)
            var accredited = PaymentService.IsAccredited(status);
            if (accredited)
                await _store.FulfillPaidOrderAsync(order.Id);

            return Ok(new
            {
                status,
                accredited,
                slug = course.Slug,
                paymentId,
                statusDetail = payment.TryGetProperty("status_detail", out var detail) ? detail.GetString() : null,
                redirect = accredited
                    ? $"/Learn?slug={course.Slug}"
                    : $"/Checkout?slug={course.Slug}&status={status}&orderId={order.Id}",
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Polling: el front consulta hasta que el QR/pago quede acreditado.
    /// </summary>
    [Authorize]
    [HttpGet("payments/order/{orderId}")]
    public async Task<IActionResult> OrderStatus(string orderId)
    {
        var userId = AuthCookie.UserId(User)!;
        var order = await _store.GetOrderByIdAsync(orderId);
        if (order == null || order.UserId != userId) return NotFound(new { error = "Orden no encontrada" });
        var course = await _store.GetCourseByIdAsync(order.CourseId);

        // Consulta a MP por paymentId o por external_reference (Wallet Brick / Checkout Pro)
        if (order.Status != "paid" && _payments.IsMercadoPagoConfigured())
        {
            try
            {
                JsonElement? payment = null;
                if (!string.IsNullOrEmpty(order.PaymentId))
                    payment = await _payments.GetPaymentAsync(order.PaymentId);
                else
                    payment = await _payments.FindLatestPaymentByExternalReferenceAsync(order.Id);

                if (payment.HasValue)
                {
                    var p = payment.Value;
                    var status = PaymentService.MapMpStatus(
                        p.TryGetProperty("status", out var st) ? st.GetString() : null);
                    var paymentId = p.TryGetProperty("id", out var id) ? id.ToString() : order.PaymentId;
                    await _store.UpdateOrderAsync(order.Id, o =>
                    {
                        o.Status = status;
                        o.PaymentId = paymentId;
                        o.StatusDetail = p.TryGetProperty("status_detail", out var sd) ? sd.GetString() : null;
                        o.PaymentMethod = p.TryGetProperty("payment_method_id", out var pm) ? pm.GetString() : o.PaymentMethod;
                    });
                    order = (await _store.GetOrderByIdAsync(order.Id))!;
                    if (PaymentService.IsAccredited(status))
                    {
                        if (await _store.GetEnrollmentAsync(userId, order.CourseId) == null)
                            await _store.FulfillPaidOrderAsync(order.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        var enrolled = course != null && await _store.GetEnrollmentAsync(userId, course.Id) != null;
        var accredited = order.Status == "paid" || enrolled;
        return Ok(new
        {
            orderId = order.Id,
            status = order.Status,
            accredited,
            paymentId = order.PaymentId,
            slug = course?.Slug,
            redirect = accredited && course != null ? $"/Learn?slug={course.Slug}" : null,
        });
    }

    [HttpGet("webhooks/mercadopago")]
    [HttpPost("webhooks/mercadopago")]
    public async Task<IActionResult> Webhook()
    {
        try
        {
            string? paymentId =
                Request.Query["data.id"].FirstOrDefault()
                ?? Request.Query["id"].FirstOrDefault();

            var topic = Request.Query["topic"].FirstOrDefault()
                ?? Request.Query["type"].FirstOrDefault();

            if (Request.ContentLength > 0 || Request.ContentType?.Contains("json") == true)
            {
                try
                {
                    using var doc = await JsonDocument.ParseAsync(Request.Body);
                    if (doc.RootElement.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("id", out var idEl))
                        paymentId ??= idEl.ToString();
                    if (doc.RootElement.TryGetProperty("type", out var typeEl))
                        topic ??= typeEl.GetString();
                    if (doc.RootElement.TryGetProperty("action", out var actionEl))
                        topic ??= actionEl.GetString();
                }
                catch
                {
                    /* body vacío o no JSON */
                }
            }

            // Solo procesamos notificaciones de pago
            if (!string.IsNullOrEmpty(topic) &&
                !topic.Contains("payment", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { ok = true });
            }

            if (string.IsNullOrEmpty(paymentId)) return Ok(new { ok = true });

            var payment = await _payments.GetPaymentAsync(paymentId);
            string? orderId = null;
            if (payment.TryGetProperty("external_reference", out var orderIdEl))
                orderId = orderIdEl.GetString();
            if (string.IsNullOrEmpty(orderId) &&
                payment.TryGetProperty("metadata", out var meta) &&
                meta.TryGetProperty("order_id", out var metaOrder))
                orderId = metaOrder.GetString();

            if (string.IsNullOrEmpty(orderId)) return Ok(new { ok = true });

            var status = PaymentService.MapMpStatus(
                payment.TryGetProperty("status", out var st) ? st.GetString() : null);

            await _store.UpdateOrderAsync(orderId, o =>
            {
                o.Status = status;
                o.PaymentId = payment.TryGetProperty("id", out var id) ? id.ToString() : paymentId;
                o.StatusDetail = payment.TryGetProperty("status_detail", out var sd) ? sd.GetString() : null;
                o.PaymentMethod = payment.TryGetProperty("payment_method_id", out var pm) ? pm.GetString() : o.PaymentMethod;
            });

            // Acreditado → habilitar curso / video / lecciones
            if (PaymentService.IsAccredited(status))
                await _store.FulfillPaidOrderAsync(orderId);

            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return Ok(new { ok = true });
        }
    }
}
