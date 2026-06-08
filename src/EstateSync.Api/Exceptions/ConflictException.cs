using System.Net;

namespace EstateSync.Api.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(string message) 
        : base(message, HttpStatusCode.Conflict) { }
}
