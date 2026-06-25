using System.Collections.Generic;
using System.Net;

namespace EstateSync.Api.Exceptions;

public sealed class ValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", HttpStatusCode.UnprocessableEntity)
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : base("A validation error occurred.", HttpStatusCode.UnprocessableEntity)
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, [error] }
        };
    }
}
