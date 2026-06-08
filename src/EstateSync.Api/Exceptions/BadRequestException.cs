using System.Net;

namespace EstateSync.Api.Exceptions;

public class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message, HttpStatusCode.BadRequest)
    {
    }
}
