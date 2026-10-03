namespace Core.Exceptions;

public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class NotFoundException(string message) : ApiException(404, message);

public class BadRequestException(string message) : ApiException(400, message);

public class UnauthorizedException(string message) : ApiException(401, message);

public class ConflictException(string message) : ApiException(409, message);

public class TooManyRequestsException(string message) : ApiException(429, message);

public class ServiceUnavailableException(string message) : ApiException(503, message);

public class ValidationException : ApiException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base(400, "One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
