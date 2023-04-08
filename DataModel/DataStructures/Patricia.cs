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

    public int Add(string key)
    {
        var current_node = Root;
        int index        = 0;
        while (index < key.Length) {
            int copy = index;
            var filtered_children =
                current_node.Children.Where(valuePair => key[copy..].StartsWith(valuePair.Key));
            foreach (var child in filtered_children) {
                current_node =  child.Value;
                index        += child.Key.Length;
                goto continueLoop;
            }

            current_node.Children[key[index..]] = new Node();
            current_node                        = current_node.Children[key[index..]];
            index                               = key.Length;
            continueLoop: ;
        }

        current_node.IsEndOfWord = true;
        return 0;
    }


    public void Clear()
    {
        Root = new Node();
    }

    public List<string> Retrieve(string prefix)
    {
        var results = new List<string>();
        var word    = new StringBuilder();

        var current_node = Root;

        var children = current_node.Children.Where(child => child.Key.CommonPrefix(prefix));

        foreach (var child in children) {
            word.Append(child.Key);
            if (child.Value.IsEndOfWord) {
                results.Add(word.ToString());
            }
        
            string new_prefix = string.IsNullOrEmpty(prefix) ? "" : string.Concat(prefix.Skip(child.Key.Length));

            Retrieve(child.Value, new_prefix, results, word);
            word.Length -= child.Key.Length;
        }
    
        return results;
    }

    static void Retrieve(Node node, string prefix, ICollection<string> results, StringBuilder word)
    {
        var children = node.Children.Where(child => child.Key.CommonPrefix(prefix));

        foreach (var child in children) {
            word.Append(child.Key);
            if (child.Value.IsEndOfWord) {
                results.Add(word.ToString());
            }

            string new_prefix = string.IsNullOrEmpty(prefix) ? "" : string.Concat(prefix.Skip(child.Key.Length));

            Retrieve(child.Value, new_prefix, results, word);
            word.Length -= child.Key.Length;
        }
    }


    IEnumerable<string> ITRie<string>.Retrieve(string prefix)
    {
        return Retrieve(prefix);
    }

    public IEnumerator<string> GetEnumerator()
    {
        var stack = new Stack<ValueTuple<Node, string?>>();
        var words = new SortedSet<string>();

        stack.Push(new ValueTuple<Node, string?>(Root, null));

        while (stack.Count > 0) {
            (var node, string? prefix) = stack.Pop();

            if (node.IsEndOfWord && prefix != null) {
                words.Add(prefix);
            }

            foreach (var child in node.Children) {
                stack.Push(new ValueTuple<Node, string>(child.Value, prefix + child.Key));
            }
        }

        foreach (string word in words) {
            yield return word;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static void PrettyString(Node node, string prefix, StringBuilder? stringBuilder)
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