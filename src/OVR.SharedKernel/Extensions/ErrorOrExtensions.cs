using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace OVR.SharedKernel.Extensions;

public static class ErrorOrExtensions
{
    public static IResult ToApiResult<T>(this ErrorOr<T> result) =>
        result.Match(
            value => Results.Ok(value),
            errors => ToApiError(errors));

    public static IResult ToCreatedResult<T>(this ErrorOr<T> result, string uri) =>
        result.Match(
            value => Results.Created(uri, value),
            errors => ToApiError(errors));

    private static IResult ToApiError(List<Error> errors)
    {
        var firstError = errors[0];
        return firstError.Type switch
        {
            ErrorType.Validation => Results.ValidationProblem(
                errors.ToDictionary(e => e.Code, e => new[] { e.Description })),
            ErrorType.NotFound => Results.NotFound(new { firstError.Code, firstError.Description }),
            ErrorType.Conflict => Results.Conflict(new { firstError.Code, firstError.Description }),
            ErrorType.Unauthorized => Results.Unauthorized(),
            _ => Results.Problem(statusCode: 500, title: firstError.Description)
        };
    }
}
