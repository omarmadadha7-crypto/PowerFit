using Microsoft.EntityFrameworkCore;
using Powerfit.Business.Exceptions;
using Xunit;

namespace Powerfit.Tests.Business;

public class ExceptionTranslatorTests
{
    [Fact]
    public void Other_exceptions_pass_through_unchanged()
    {
        var ex = new InvalidOperationException("boom");

        Assert.Same(ex, ExceptionTranslator.Translate(ex));
    }

    [Fact]
    public void A_concurrency_failure_becomes_a_conflict()
    {
        var original = new DbUpdateConcurrencyException("row changed");

        var translated = ExceptionTranslator.Translate(original);

        Assert.IsType<ConflictException>(translated);
        Assert.Same(original, translated.InnerException);
    }

    [Fact]
    public void A_plain_db_update_error_is_not_hidden()
    {
        var ex = new DbUpdateException("check constraint", new InvalidOperationException());

        Assert.Same(ex, ExceptionTranslator.Translate(ex));
    }
}
