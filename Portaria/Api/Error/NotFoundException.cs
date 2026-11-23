
namespace Api.Error;

public class NotFoundException : Exception
{
    public NotFoundException(string message) 
        : base(message)
    {
    }
}