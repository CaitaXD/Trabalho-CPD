namespace DataModel.DataStructures.Generic;

public static class PatriciaTrieExtension
{
    public static bool StartsWith<TSymbol>(this IEnumerable<TSymbol> a, IEnumerable<TSymbol> b)
    {
        foreach (var (a_item, b_item) in a.Zip(b)) {
            if (!a_item.Equals(b_item)) {
                return false;
            }
        }

        return true;
    }
}