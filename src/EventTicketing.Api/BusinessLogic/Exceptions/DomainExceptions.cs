namespace EventTicketing.BusinessLogic.Exceptions;

// Thrown by the business layer; the API maps each to an HTTP status via middleware.

/// <summary>Requested entity does not exist → 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Request is well-formed but violates a business rule → 400.</summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

/// <summary>State changed under us / seat taken by someone else → 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
