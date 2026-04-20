namespace PhanMemKeToan.Application.Common.Exceptions;

public class NotFoundException(string name, object key)
    : Exception($"Entity \"{name}\" ({key}) was not found.")
{
    public string EntityName { get; } = name;
    public object Key { get; } = key;
}
