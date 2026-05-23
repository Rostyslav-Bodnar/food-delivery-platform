namespace DF.MenuService.Contracts.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public static NotFoundException For(string resource, object id)
        => new($"{resource} '{id}' not found");
}
