using System;
using System.Collections.Generic;

namespace EstateSync.Api.Common;

public record PagedResponse<T> : ApiResponse<IEnumerable<T>>
{
    public int Page { get; init; }
    public int Limit { get; init; }
    public int Total { get; init; }
    public int Pages => (int)Math.Ceiling((double)Total / Limit);
    public bool HasNext => Page < Pages;
}
