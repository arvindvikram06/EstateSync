using System.Net;

namespace EstateSync.Api.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string name, object key) 
        : base($"Entity \"{name}\" ({key}) was not found.", HttpStatusCode.NotFound) { }
}
