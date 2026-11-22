using System.Collections.Generic;

/// <summary>
/// Stand-in for the helper the C# compiler generated for single-element collection expressions
/// (<c>[tip]</c>) in the original build. The decompiler keeps the calls but not a usable class, so this
/// provides the same shape: a list holding exactly one item.
/// </summary>
internal sealed class _003C_003Ez__ReadOnlySingleElementList<T> : List<T>
{
    public _003C_003Ez__ReadOnlySingleElementList(T item)
        : base(1)
    {
        Add(item);
    }
}