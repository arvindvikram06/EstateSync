using System.Net;

namespace EstateSync.Api.Exceptions;

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) 
        : base(message, HttpStatusCode.Unauthorized) { }
}
