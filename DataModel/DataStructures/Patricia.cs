using System.Collections;
using System.Numerics;
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
        var node = Root;
        for (int i = 0; i < key.Length;) {
            int offset = i;
            var children = node.Children.Where(valuePair => key[offset..].StartsWith(valuePair.Key));

            var arr = children.ToArray();
            foreach (var (word, child) in arr) {
                node =  child;
                i        += word.Length;
                goto continueLoop;
            }

            node.Children[key[i..]] = new Node();
            node                        = node.Children[key[i..]];
            i                               = key.Length;

            continueLoop: ;
        }

        node.IsEndOfWord = true;
        Root.WriteEncodings();

        return node.Id;
    }

    Node? Find(string word)
    {
        return Root.Find(word);
    }

    public int Encode(string word)
    {
        return Find(word)?.Id ?? -1;
    }

    public string Decode(int encoded)
    {
        return Root.Decode(encoded);
    }

    public void Clear()
    {
        Root = new Node();
    }

    public IEnumerable<string> Retrieve(string prefix)
    {
        return Root.Retrieve(prefix);
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

    public IEnumerable<Node> Traverse()
    {
        return Root.Traverse();
    }

    public string PrettyString()
    {
        return Root.PrettyString();
    }

    public class Node : IEnumerable<(string, Node)>
    {
        public IDictionary<string, Node> Children    { get; private set; }
        public bool                      IsEndOfWord { get; set; }

        public int Id { get; set; }

        public Node()
        {
            Children    = new SortedList<string, Node>();
            IsEndOfWord = false;
        }

        public IEnumerable<Node> Traverse()
        {
            if (IsEndOfWord) {
                yield break;
            }

            foreach (var (_, child) in Children) {
                foreach (var n_node in child.Traverse()) {
                    yield return n_node;
                }
            }
        }

        public IEnumerator<(string, Node)> GetEnumerator()
        {
            foreach (var (key, value) in Children) {
                yield return (key, value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Node? Find(ReadOnlySpan<char> word)
        {
            if (word.Length == 0) {
                return this;
            }

            if (IsEndOfWord) {
                return null;
            }

            foreach (var (char_value, child) in Children) {
                if (word.StartsWith(char_value)) {
                    if (word.Length == char_value.Length) {
                        return child;
                    }
                    else {
                        return child.Find(word[char_value.Length..]);
                    }
                }
            }

            return null;
        }

        public IEnumerable<string> Retrieve(string prefix)
        {
            return Retrieve(new StringBuilder(prefix));
        }

        IEnumerable<string> Retrieve(StringBuilder? prefix = null)
        {
            prefix ??= new StringBuilder();

            if (IsEndOfWord) {
                yield return prefix.ToString();
            }

            foreach (var (str, child) in Children) {
                prefix.Append(str);
                foreach (string word in child.Retrieve(prefix)) {
                    yield return word;
                }
            }
        }

        public string PrettyString()
        {
            var string_builder = new StringBuilder();
            PrettyString("", string_builder);
            return string_builder.ToString();
        }

        void PrettyString(string prefix, StringBuilder? stringBuilder)
        {
            stringBuilder ??= new StringBuilder();
            foreach (var child in Children) {
                stringBuilder.Append(prefix);
                foreach (char item in child.Key) {
                    stringBuilder.Append(item);
                }

                stringBuilder.Append(child.Value.IsEndOfWord ? "*" : "");
                stringBuilder.Append(Environment.NewLine);
                child.Value.PrettyString(prefix + "  ", stringBuilder);
            }
        }

        public string Decode(int encoded, StringBuilder? sb = null)
        {
            sb ??= new StringBuilder();

            foreach (var (word, child) in Children) {
                if (child.Id == encoded) {
                    sb.Append(word);
                    return child.Decode(encoded, sb);
                }
            }

            return sb.ToString();
        }

        public void WriteEncodings(int encoding = default)
        {
            int counter = 1;

            foreach (var (_, child) in Children) {
                encoding =  (encoding << (int)Math.Log2(counter)) | counter;
                child.Id =  encoding;
                counter  += 1;
                child.WriteEncodings(encoding << 1);
            }
        }
    }
}