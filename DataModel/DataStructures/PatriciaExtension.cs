using System.Buffers;
using System.Collections;
using System.Text;

namespace DataModel.DataStructures;

public static partial class PatriciaExtension
{
    public static void Write(this Patricia patricia, Stream stream, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        var writer = new BinaryWriter(stream);
        WriteNode(patricia.Root, writer, encoding);
    }

    public static void WriteNode(Patricia.Node node, BinaryWriter writer, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        writer.Write(node.IsEndOfWord);
        writer.Write(node.Id);

        // Write the number of children for this node
        writer.Write(node.Children.Count);

        // Write each child recursively
        foreach (var (word, child) in node) {
            string keys = string.Concat(word);
            writer.Write(keys);
            WriteNode(child, writer);
        }
    }

    public static void Read(this Patricia patricia, Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        if (stream.Length == 0) {
            return;
        }

        var reader = new BinaryReader(stream);
        var root   = ReadNode(reader);
        
        
        var new_root = MergeNodes(patricia.Root, root);
        
        patricia.Root = new_root;
        patricia.Root.WriteEncodings();
    }
    
    public static Patricia MergeNodes(Patricia a, Patricia b)
    {
        var merged = new Patricia
        {
            Root = MergeNodes(a.Root, b.Root)
        };
        return merged;
    }
    
    public static Patricia.Node MergeNodes(Patricia.Node a, Patricia.Node b)
    {
        var merged_node = new Patricia.Node();

        // Merge the children of a and b
        foreach (var child_a in a.Children) {
            if (b.Children.TryGetValue(child_a.Key, out var child_b)) {
                // Both a and b have a child with the same key, so merge them recursively
                merged_node.Children[child_a.Key] = MergeNodes(child_a.Value, child_b);
            }
            else {
                // Only a has a child with this key, so add it to the merged node
                merged_node.Children[child_a.Key] = child_a.Value;
            }
        }

        // Add any children of b that were not already added from a
        foreach (var child_b in b.Children) {
            if (!merged_node.Children.ContainsKey(child_b.Key)) {
                merged_node.Children[child_b.Key] = child_b.Value;
            }
        }

        merged_node.IsEndOfWord = a.IsEndOfWord || b.IsEndOfWord;

        return merged_node;
    }
    public static Patricia.Node ReadNode(BinaryReader reader, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var node = new Patricia.Node
        {
            IsEndOfWord = reader.ReadBoolean(),
            Id = reader.ReadInt32()
        };
        if (reader.BaseStream.Position == reader.BaseStream.Length) {
            return node;
        }
        int children_count = reader.ReadInt32();
        for (int i = 0; i < children_count; i++)
        {
            string key = reader.ReadString();
            
            if (reader.BaseStream.Position == reader.BaseStream.Length) {
                node.Children.Add(key, new Patricia.Node());
                return node;
            }
            
            var child = ReadNode(reader, encoding);
            node.Children.Add(key, child);
        }

        return node;
    }
    public static IEnumerable<Patricia.Node> ReadNodes(Stream fileStream, Encoding? encoding = null)
    {
        var reader = new BinaryReader(fileStream, encoding ?? Encoding.UTF8);
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var node = ReadNode(reader, encoding);
            yield return node;
        }
    }

    
    public static bool CommonPrefix(this string a, string b)
    {
        for (int i = 0; i < Math.Min(a.Length, b.Length); i++) {
            if (a[i] != b[i]) {
                return false;
            }
        }

        return true;
    }
}