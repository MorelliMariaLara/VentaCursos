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
    });

    public record SlugBody(string? Slug);
    public record ProcessBody(string? OrderId, bool Simulate, JsonElement? FormData);

    [Authorize]
    [HttpPost("payments/preference")]
    public async Task<IActionResult> Preference([FromBody] SlugBody body)
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
                return Ok(new { orderId = order.Id, simulateOnly = true, preferenceId = (string?)null });

            var pref = await _payments.CreatePreferenceAsync(new PreferenceInput(
                order.Id, course.Title, course.Price, course.Currency,
                AuthCookie.Email(User)!, course.Id, course.Slug));
            var prefId = pref.GetProperty("id").ToString();
            await _store.UpdateOrderAsync(order.Id, o => o.PreferenceId = prefId);
            return Ok(new { orderId = order.Id, preferenceId = prefId, simulateOnly = false });
        }
        catch (InvalidOperationException ex) when (ex.Message == "ALREADY_OWNED")
        {
            return Conflict(new { error = "Ya comprado" });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return StatusCode(500, new { error = "No se pudo crear la preferencia" });
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

        try
        {
            if (body.Simulate)
            {
                if (!_payments.AllowSimulatePayments())
                    return BadRequest(new { error = "Simulación deshabilitada" });
                await _store.UpdateOrderAsync(order.Id, o =>
                {
                    o.Simulated = true;
                    o.PaymentMethod = "simulate";
                });
                await _store.FulfillPaidOrderAsync(order.Id);
                return Ok(new
                {
                    status = "paid",
                    slug = course.Slug,
                    redirect = $"/Learn?slug={course.Slug}",
                });
            }

            if (!_payments.IsMercadoPagoConfigured())
                return BadRequest(new { error = "Mercado Pago no configurado" });

            object formData = body.FormData.HasValue
                ? JsonSerializer.Deserialize<object>(body.FormData.Value.GetRawText())!
                : new { };
            var payment = await _payments.CreatePaymentAsync(formData, order.Id);
            var status = PaymentService.MapMpStatus(
                payment.TryGetProperty("status", out var st) ? st.GetString() : null);
            await _store.UpdateOrderAsync(order.Id, o =>
            {
                o.Status = status;
                o.PaymentId = payment.TryGetProperty("id", out var id) ? id.ToString() : null;
                o.PaymentMethod = payment.TryGetProperty("payment_method_id", out var pm) ? pm.GetString() : null;
                o.StatusDetail = payment.TryGetProperty("status_detail", out var sd) ? sd.GetString() : null;
                if (payment.TryGetProperty("payer", out var payer) &&
                    payer.TryGetProperty("email", out var email))
                    o.PayerEmail = email.GetString();
            });
            if (status == "paid") await _store.FulfillPaidOrderAsync(order.Id);
            return Ok(new
            {
                status,
                slug = course.Slug,
                redirect = status == "paid"
                    ? $"/Learn?slug={course.Slug}"
                    : $"/Checkout?slug={course.Slug}&status={status}",
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return StatusCode(500, new { error = "Error procesando pago" });
        }
    }

    [HttpGet("webhooks/mercadopago")]
    [HttpPost("webhooks/mercadopago")]
    public async Task<IActionResult> Webhook()
    {
        try
        {
            string? paymentId = Request.Query["data.id"].FirstOrDefault()
                ?? Request.Query["id"].FirstOrDefault();
            if (Request.ContentLength > 0)
            {
                using var doc = await JsonDocument.ParseAsync(Request.Body);
                if (doc.RootElement.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("id", out var idEl))
                    paymentId ??= idEl.ToString();
            }
            if (string.IsNullOrEmpty(paymentId)) return Ok(new { ok = true });

            var payment = await _payments.GetPaymentAsync(paymentId);
            if (!payment.TryGetProperty("external_reference", out var orderIdEl))
                return Ok(new { ok = true });
            var orderId = orderIdEl.GetString();
            if (string.IsNullOrEmpty(orderId)) return Ok(new { ok = true });

            var status = PaymentService.MapMpStatus(
                payment.TryGetProperty("status", out var st) ? st.GetString() : null);
            await _store.UpdateOrderAsync(orderId, o =>
            {
                o.Status = status;
                o.PaymentId = payment.TryGetProperty("id", out var id) ? id.ToString() : null;
                o.StatusDetail = payment.TryGetProperty("status_detail", out var sd) ? sd.GetString() : null;
            });
            if (status == "paid") await _store.FulfillPaidOrderAsync(orderId);
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return Ok(new { ok = true });
        }
    }
}
