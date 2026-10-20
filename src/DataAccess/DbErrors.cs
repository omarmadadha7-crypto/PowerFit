using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public enum DbErrorKind
{
    /// <summary>Not a database conflict (or not recognised).</summary>
    Other,

    /// <summary>UNIQUE index / constraint (SQL Server 2601, 2627).</summary>
    UniqueViolation,

    /// <summary>FOREIGN KEY (SQL Server 547 on an FK_ constraint): the row is in use or was removed meanwhile.</summary>
    ReferenceViolation,

    /// <summary>The row was changed or deleted by someone else (rowversion check).</summary>
    ConcurrencyConflict
}

/// <summary>
/// Keeps the SQL Server specifics inside the data tier: the business tier only sees a <see cref="DbErrorKind"/>.
/// </summary>
public static class DbErrors
{
    public static DbErrorKind Classify(Exception ex)
    {
        if (ex is DbUpdateConcurrencyException) return DbErrorKind.ConcurrencyConflict;
        if (ex is not DbUpdateException) return DbErrorKind.Other;

        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is not SqlException sql) continue;

            switch (sql.Number)
            {
                case 2601:
                case 2627:
                    return DbErrorKind.UniqueViolation;

                // 547 is used for FK and CHECK violations. Only FK ones are "conflicts" (CHECK = a bug in our code).
                // The constraint name is used instead of the (localised) message text.
                case 547 when sql.Message.Contains("FK_", StringComparison.Ordinal):
                    return DbErrorKind.ReferenceViolation;
            }
        }

        return DbErrorKind.Other;
    }
}
