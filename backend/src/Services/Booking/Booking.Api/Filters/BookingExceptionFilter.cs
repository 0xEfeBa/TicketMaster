using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Booking.Api.Filters;

public class BookingExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DbUpdateConcurrencyException)
            return;

        context.Result = new ObjectResult("Kayıt aynı anda güncellendi. Lütfen tekrar deneyin.")
        {
            StatusCode = StatusCodes.Status409Conflict
        };
        context.ExceptionHandled = true;
    }
}
