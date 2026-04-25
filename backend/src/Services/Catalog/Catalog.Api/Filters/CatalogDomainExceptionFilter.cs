using Catalog.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Catalog.Api.Filters;

public class CatalogDomainExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is DbUpdateConcurrencyException)
        {
            context.Result = new ObjectResult("The record was updated concurrently. Please retry.")
            {
                StatusCode = StatusCodes.Status409Conflict
            };
            context.ExceptionHandled = true;
            return;
        }

        if (context.Exception is not CatalogDomainException d)
            return;

        context.Result = d.IsAccessDenied
            ? new ObjectResult(d.Message) { StatusCode = StatusCodes.Status403Forbidden }
            : new ObjectResult(d.Message) { StatusCode = StatusCodes.Status400BadRequest };

        context.ExceptionHandled = true;
    }
}
