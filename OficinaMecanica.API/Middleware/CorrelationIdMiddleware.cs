using Serilog.Context;

namespace OficinaMecanica.API.Middleware
{
    /// <summary>
    /// Le o header X-Correlation-Id da requisicao (ou gera um novo), propaga pra resposta
    /// e injeta no contexto de log do Serilog, permitindo correlacionar todas as linhas de
    /// log de uma mesma requisicao (e entre servicos, se o header for repassado).
    /// </summary>
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
                ? existing.ToString()
                : Guid.NewGuid().ToString();

            context.Response.Headers[HeaderName] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }
}
