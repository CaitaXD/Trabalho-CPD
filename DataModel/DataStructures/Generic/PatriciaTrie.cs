using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

namespace DataModel.DataStructures.Generic;

public class PatriciaTrie<TSymbol>
    : IEnumerable<IEnumerable<TSymbol>>
    where TSymbol : IComparable<TSymbol>
{
    public Node Root { get; internal set; }

    public PatriciaTrie()
    {
        Root = new Node();
    }

    public void Add(IEnumerable<TSymbol> key)
    {
        var current_node = Root;
        int index        = 0;
        var key_list     = key.ToArray();
        while (index < key_list.Length) {
            int copy = index;
            var filtered_children =
                current_node.Children.Where(valuePair => key_list.Skip(copy).StartsWith(valuePair.Key));
            foreach (var child in filtered_children) {
                current_node =  child.Value;
                index        += child.Key.Count();
                goto continueLoop;
            }

            current_node.Children[key_list.Skip(index)] = new Node();
            current_node                                = current_node.Children[key_list.Skip(index)];
            index                                       = key_list.Length;
            continueLoop: ;
        }

        current_node.IsEndOfWord = true;
    }

    public List<IEnumerable<TSymbol>> Retrieve(IEnumerable<TSymbol> prefix)
    {
        return RetrieveRecursive(Root, prefix);
    }

    List<IEnumerable<TSymbol>> RetrieveRecursive(Node node, IEnumerable<TSymbol> prefix,
        List<IEnumerable<TSymbol>>?                   result = null)
    {
        result ??= new List<IEnumerable<TSymbol>>();

        var prefix_array = prefix.ToArray();

        foreach (var child in node.Children.Where(child => child.Key.StartsWith(prefix_array))) {
            result.Add(child.Key);
            var new_prefix = prefix_array.Skip(child.Key.Count());

            RetrieveRecursive(child.Value, new_prefix, result);
        }

        return result;
    }

    public IEnumerator<IEnumerable<TSymbol>> GetEnumerator()
    {
        return Retrieve(Enumerable.Empty<TSymbol>()).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    static string PrettyString(Node node, string prefix)
    {
        var sb = new StringBuilder();
        foreach (var child in node.Children) {
            sb.Append(prefix);
            foreach (var item in child.Key) {
                sb.Append(item);
            }

            sb.Append(child.Value.IsEndOfWord ? "*" : "");
            sb.Append(Environment.NewLine);
            sb.Append(PrettyString(child.Value, prefix + "  "));
        }

        return sb.ToString();
    }

    public string PrettyString()
    {
        return PrettyString(Root, "");
    }

    public class Node
    {
        static readonly SequenceComparer Comparer = new();

        public SortedDictionary<IEnumerable<TSymbol>, Node> Children    { get; private set; }
        public bool                                         IsEndOfWord { get; set; }

        public Node()
        {
            Children    = new SortedDictionary<IEnumerable<TSymbol>, Node>(Comparer);
            IsEndOfWord = false;
        }
    }

    class SequenceComparer : IComparer<IEnumerable<TSymbol>>
    {
        public int Compare(IEnumerable<TSymbol>? a, IEnumerable<TSymbol>? b)
        {
            var a_list = a?.ToArray() ?? Array.Empty<TSymbol>();
            var b_list = b?.ToArray() ?? Array.Empty<TSymbol>();

            foreach (var (a_item, b_item) in a_list.Zip(b_list)) {
                int result = a_item.CompareTo(b_item);
                if (result != 0) {
                    return result;
                }
            }

            return 0;
        }
    }
}