using System.Collections;
using System.Reflection.Metadata;
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
        var result       = new List<string>();
        var current_node = Root;

        foreach (var (word, child) in current_node.Children.Where(child => child.Key.CommonPrefix(prefix))) {
            foreach (string new_word in RetrieveWordsFromNode(child, word)) {
                if (new_word.StartsWith(prefix)) {
                    result.Add(new_word);
                }
            }
        }

        return result;
    }

    static IEnumerable<string> RetrieveWordsFromNode(Node node, string prefix)
    {
        if (node.IsEndOfWord) {
            yield return prefix;
        }

        foreach (var child in node.Children) {
            string child_prefix = prefix + child.Key;
            foreach (string word in RetrieveWordsFromNode(child.Value, child_prefix)) {
                yield return word;
            }
        }
    }

    IEnumerable<string> ITRie<string>.Retrieve(string prefix)
    {
        return Retrieve(prefix);
    }

    public IEnumerator<string> GetEnumerator()
    {
        return Retrieve("").GetEnumerator();
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