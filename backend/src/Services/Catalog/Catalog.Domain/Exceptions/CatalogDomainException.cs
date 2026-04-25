namespace Catalog.Domain.Exceptions;

public class CatalogDomainException : Exception
{
    /// <summary>
    /// API katmanı 403 üretmek için (sahiplik / yetki).
    /// </summary>
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
