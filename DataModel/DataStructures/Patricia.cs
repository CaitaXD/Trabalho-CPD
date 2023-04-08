using System.Collections;
using System.Text;

namespace DataModel.DataStructures;

public class Patricia : ITRie<string>
{
    public Node Root { get; internal set; }

    public Patricia()
    {
        Root = new Node();
    }

    public void Add(string key)
    {
        var current_node = Root;
        int index        = 0;
        while (index < key.Length) {
            int copy = index;
            var filtered_children =
                current_node.Children.Where(valuePair => key[copy..].StartsWith(valuePair.Key));
            foreach (var child in filtered_children) {
                current_node =  child.Value;
                index        += child.Key.Count();
                goto continueLoop;
            }

            current_node.Children[key[index..]] = new Node();
            current_node                        = current_node.Children[key[index..]];
            index                               = key.Length;
            continueLoop: ;
        }

        current_node.IsEndOfWord = true;
    }

    public List<string> Retrieve(string prefix)
    {
        return RetrieveRecursive(Root, prefix);
    }

    IEnumerable<string> ITRie<string>.Retrieve(string prefix)
    {
        return RetrieveRecursive(Root, prefix);
    }

    static List<string> RetrieveRecursive(Node node, string prefix,
        List<string>?                          result = null)
    {
        result ??= new List<string>();

        foreach (var child in node.Children.Where(child => child.Key.CommonPrefix(prefix))) {
            result.Add(child.Key);
            string new_prefix = string.IsNullOrEmpty(prefix) ? "" : string.Concat(prefix.Skip(child.Key.Length));

            RetrieveRecursive(child.Value, new_prefix, result);
        }

        return result;
    }

    public IEnumerator<string> GetEnumerator()
    {
        return Retrieve("").GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static void PrettyString(Node node, string prefix, StringBuilder stringBuilder)
    {
        stringBuilder ??= new StringBuilder();
        foreach (var child in node.Children) {
            stringBuilder.Append(prefix);
            foreach (char item in child.Key) {
                stringBuilder.Append(item);
            }

            stringBuilder.Append(child.Value.IsEndOfWord ? "*" : "");
            stringBuilder.Append(Environment.NewLine);
            PrettyString(child.Value, prefix + "  ", stringBuilder);
        }
    }

    public string PrettyString()
    {
        var string_builder = new StringBuilder();
        PrettyString(Root, "", string_builder);
        
        return string_builder.ToString();
    }

    public class Node
    {
        public IDictionary<string, Node> Children    { get; private set; }
        public bool                      IsEndOfWord { get; set; }

        public Node()
        {
            Children    = new SortedDictionary<string, Node>();
            IsEndOfWord = false;
        }
    }
}