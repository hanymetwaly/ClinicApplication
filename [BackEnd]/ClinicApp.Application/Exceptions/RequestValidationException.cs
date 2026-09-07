namespace ClinicApp.Application.Exceptions;

public class RequestValidationException(string message) : Exception(message);
