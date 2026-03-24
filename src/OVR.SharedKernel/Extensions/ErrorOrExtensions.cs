using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OVR.SharedKernel.I18n;

namespace OVR.SharedKernel.Extensions;

public static class ErrorOrExtensions
{
    public static IResult ToApiResult<T>(this ErrorOr<T> result, HttpContext httpContext) =>
        result.Match(
            value => TypedResults.Ok(value),
            errors => errors.ToApiError(httpContext));

    public static IResult ToCreatedResult<T>(this ErrorOr<T> result, string uri, HttpContext httpContext) =>
        result.Match(
            value => TypedResults.Created(uri, value),
            errors => errors.ToApiError(httpContext));

    public static IResult ToApiError(this List<Error> errors, HttpContext httpContext)
    {
        var translator = httpContext.RequestServices.GetService<ITranslationService>();
        var language = LanguageDetector.DetectLanguage(httpContext);
        var firstError = errors[0];

        var (statusCode, title, type) = MapErrorType(firstError.Type);

        var translatedErrors = errors.Select(e => new
        {
            code = e.Code,
            detail = TranslateError(e, language, translator)
        }).ToArray();

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = translatedErrors[0].detail,
        };

        problemDetails.Extensions["errorCode"] = firstError.Code;
        problemDetails.Extensions["errors"] = translatedErrors;

        return TypedResults.Problem(problemDetails);
    }

    private static string TranslateError(Error error, string language, ITranslationService? translator)
    {
        if (translator is null)
            return error.Description;

        return translator.Translate(error.Code, language, error.Description, error.Metadata);
    }

    private static (int StatusCode, string Title, string Type) MapErrorType(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation Error",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found",
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict",
                "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized",
                "https://tools.ietf.org/html/rfc9110#section-15.5.2"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden",
                "https://tools.ietf.org/html/rfc9110#section-15.5.4"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };
}
