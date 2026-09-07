namespace AgencyFlow.Exceptions;

public sealed class ForbiddenException(string message) : Exception(message);
