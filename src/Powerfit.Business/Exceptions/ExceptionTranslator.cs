using System;
using DataAccess;

namespace Powerfit.Business.Exceptions;

/// <summary>
/// Turns database conflicts (duplicate value, row in use, row changed by someone else) into
/// <see cref="ConflictException"/>, so races that slip past the pre-checks give 409 instead of 500.
/// Anything else is returned unchanged.
/// </summary>
public static class ExceptionTranslator
{
    public static Exception Translate(Exception ex) => DbErrors.Classify(ex) switch
    {
        DbErrorKind.UniqueViolation =>
            new ConflictException("A record with the same unique value already exists.", ex),

        DbErrorKind.ReferenceViolation =>
            new ConflictException(
                "The record is in use by other data (or was removed meanwhile). Reload and try again; deactivate it instead of deleting.", ex),

        DbErrorKind.ConcurrencyConflict =>
            new ConflictException("The record was changed or deleted by someone else. Reload and try again.", ex),

        _ => ex
    };
}
