namespace Catalog.Domain.Exceptions;

public class CatalogDomainException : Exception
{
    public bool IsAccessDenied { get; }

    public CatalogDomainException(string message, bool isAccessDenied = false) : base(message)
    {
        IsAccessDenied = isAccessDenied;
    }

    public CatalogDomainException(string message, Exception innerException) : base(message, innerException)
    {
        IsAccessDenied = false;
    }
}
