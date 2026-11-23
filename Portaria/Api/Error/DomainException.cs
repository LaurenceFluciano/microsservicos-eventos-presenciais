namespace Api.Error;

public class DomainException : Exception 
{
    public string? Code { get; }
    public IReadOnlyCollection<string> Errors { get; }

    public DomainException(string message, string? code = null) 
        : base(message)
    {
        Code = code;
        Errors = new List<string> { message };
    }

    public DomainException(string message, List<string> errors, string? code = null) 
        : base(message)
    {
        Code = code;
        Errors = errors ?? new List<string>();
    }

    public DomainException(string message, Exception innerException, string? code = null) 
        : base(message, innerException)
    {
        Code = code;
        Errors = new List<string> { message };
    }

    public static void When(bool hasError, string message, string? code = null)
    {
        if (hasError)
            throw new DomainException(message, code);
    }


    public static void When(List<string> errors, string message = "Ocorreram erros de validação.", string? code = null)
    {
        if (errors != null && errors.Any())
            throw new DomainException(message, errors, code);
    }
}
