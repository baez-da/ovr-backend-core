using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using OVR.SharedKernel.I18n;

namespace OVR.Api.Configuration;

internal sealed class ValidationExceptionHandler(
    ITranslationService translationService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
            return false;

        var language = LanguageDetector.DetectLanguage(httpContext);

        var errors = validationException.Errors
            .GroupBy(e => TranslateFieldName(e.PropertyName, language))
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => TranslateValidationMessage(e, language)).ToArray());

        var problemDetails = new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private string TranslateValidationMessage(
        FluentValidation.Results.ValidationFailure failure, string language)
    {
        var errorCode = failure.ErrorCode;
        var key = $"Validation.{errorCode}";

        var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Use original PropertyName (not humanized) for field name translation
        parameters["propertyName"] = TranslateFieldName(failure.PropertyName, language);

        if (failure.FormattedMessagePlaceholderValues is not null)
        {
            foreach (var kvp in failure.FormattedMessagePlaceholderValues)
            {
                if (kvp.Value is not null && kvp.Key != "PropertyName" && kvp.Key != "PropertyValue")
                {
                    parameters[kvp.Key] = kvp.Value;
                }
            }
        }

        return translationService.Translate(key, language, failure.ErrorMessage, parameters);
    }

    private string TranslateFieldName(string propertyName, string language) =>
        translationService.TranslateFieldName(propertyName, language);
}
