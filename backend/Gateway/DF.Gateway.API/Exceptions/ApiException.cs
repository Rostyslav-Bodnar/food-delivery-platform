using System.Net;

namespace DF.Gateway.API.Exceptions;

public abstract class ApiException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public class BadRequestException(string message) : ApiException(message, (int)HttpStatusCode.BadRequest);

public class NotFoundException(string message) : ApiException(message, (int)HttpStatusCode.NotFound);

public class UnauthorizedException(string message) : ApiException(message, (int)HttpStatusCode.Unauthorized);

public class ForbiddenException(string message) : ApiException(message, (int)HttpStatusCode.Forbidden);
