using System;
using System.Numerics;

namespace easpace.Models;

public interface IGrowthTargetEntry
{
    public Guid Id { get; }
    object? Value { get; }
    DateTime Date { get; }
}

public class GrowthTargetEntry<T> : IGrowthTargetEntry where T : INumber<T>
{
    public Guid Id { get; init; }
    public T? Value { get; init; }
    public DateTime Date { get; init; }
    
    object? IGrowthTargetEntry.Value => Value;
}
