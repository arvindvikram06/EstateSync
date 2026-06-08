using System.Net;

namespace EstateSync.Api.Exceptions;

public class ForbiddenException : AppException
{
    public ForbiddenException(string message) 
        : base(message, HttpStatusCode.Forbidden) { }
}
