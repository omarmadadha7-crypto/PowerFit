using System;
using System.Collections.Generic;
using System.Linq;
using DataAccess.Repositories;

namespace Powerfit.Business.DTOs.Common;

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasPrevious,
    bool HasNext)
{
    /// <summary>Converts a repository page into an API page, mapping every item.</summary>
    public static PagedResponse<T> From<TSource>(PagedResult<TSource> source, Func<TSource, T> map)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(map);

        return new PagedResponse<T>(
            source.Items.Select(map).ToList(),
            source.TotalCount,
            source.Page,
            source.PageSize,
            source.TotalPages,
            source.HasPrevious,
            source.HasNext);
    }
}
