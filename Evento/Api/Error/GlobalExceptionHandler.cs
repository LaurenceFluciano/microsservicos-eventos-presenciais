using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Error;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, titulo, mensagem) = exception switch
        {
            NotFoundException e => (
                StatusCodes.Status404NotFound,
                "Recurso não encontrado",
                e.Message
            ),

            DomainException e => (
                StatusCodes.Status400BadRequest,
                "Violação de regra de negócio",
                e.Message
            ),

            HttpRequestException => (
                StatusCodes.Status503ServiceUnavailable,
                "Serviço externo indisponível",
                "Não foi possível concluir a comunicação com um serviço externo."
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Erro interno",
                "Ocorreu um erro interno."
            )
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Erro inesperado durante a requisição."
            );
        }

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = mensagem,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = status;

        // É um formato padronizado para representar erros em APIs HTTP, definido pela RFC 9457.

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken
        );

        return true;
    }
}