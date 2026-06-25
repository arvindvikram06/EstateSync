using System;
using System.Net;

namespace EstateSync.Api.Exceptions;

public abstract class AppException : Exception
{
    public HttpStatusCode StatusCode { get; }

    protected AppException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError) : base(message)
    {
        StatusCode = statusCode;
    }

}