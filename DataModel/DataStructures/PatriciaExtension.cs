using System.Buffers;
using System.Collections;
using System.Text;

namespace DataModel.DataStructures;

public static partial class PatriciaExtension
{
    public static Span<byte> Serialize(this Patricia patricia, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        SerializeNodeRecursive(patricia.Root, writer, encoding);

        byte[] bytes = stream.ToArray();

        return bytes;
    }

    public static void SerializeNodeRecursive(Patricia.Node node, BinaryWriter writer, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        writer.Write(node.IsEndOfWord);

        // Write the number of children for this node
        writer.Write(node.Children.Count);

        // Write each child recursively
        foreach (var child in node.Children) {
            string key_array = string.Concat(child.Key);
            writer.Write(key_array.Length);
            byte[] bytes = encoding.GetBytes(key_array);
            writer.BaseStream.Write(bytes);
            SerializeNodeRecursive(child.Value, writer);
        }
    }

    public static void Deserialize(this Patricia patricia, Span<byte> bytes,
        Encoding?                                encoding = null)
    {
        encoding ??= Encoding.UTF8;

        using var stream = new MemoryStream(bytes.ToArray());
        using var reader = new BinaryReader(stream);

        patricia.Root = DeserializeNodeRecursive(reader, encoding);

        static Patricia.Node DeserializeNodeRecursive(BinaryReader reader, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;


            var node = new Patricia.Node
            {
                IsEndOfWord = reader.ReadBoolean()
            };

            // Read the number of children for this node
            int num_children = reader.ReadInt32();

            // Read each child recursively
            for (int i = 0; i < num_children; i++) {
                int        key_length   = reader.ReadInt32();
                Span<byte> symbol_bytes = new byte[key_length];
                reader.BaseStream.ReadExactly(symbol_bytes);
                string word = encoding.GetString(symbol_bytes);
                node.Children[word] = DeserializeNodeRecursive(reader);
            }

            return node;
        }
    }

    public static void WriteToFile(this Patricia patricia,               string    filePath,
        FileMode                                 mode = FileMode.Create, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        using var writer = new BinaryWriter(File.Open(filePath, mode));
        WriteNodeRecursive(patricia.Root, writer, encoding);

        static void WriteNodeRecursive(Patricia.Node node, BinaryWriter writer, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;

            writer.Write(node.IsEndOfWord);

            // Write the number of children for this node
            writer.Write(node.Children.Count);

            // Write each child recursively
            foreach (var child in node.Children) {
                string key_array = string.Concat(child.Key);
                writer.Write(key_array.Length);
                byte[] bytes = encoding.GetBytes(key_array);
                writer.BaseStream.Write(bytes);
                WriteNodeRecursive(child.Value, writer);
            }
        }
    }

    public static void ReadFromFile(this Patricia patricia, FileStream fileStream, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        using var reader = new BinaryReader(fileStream);
        patricia.Root = ReadNodeRecursive(reader);

        static Patricia.Node ReadNodeRecursive(BinaryReader reader, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;

            var node = new Patricia.Node
            {
                IsEndOfWord = reader.ReadBoolean()
            };

            // Read the number of children for this node
            int num_children = reader.ReadInt32();

            // Read each child recursively
            for (int i = 0; i < num_children; i++) {
                int        key_length   = reader.ReadInt32();
                Span<byte> symbol_bytes = new byte[key_length];
                reader.BaseStream.ReadExactly(symbol_bytes);
                string word = encoding.GetString(symbol_bytes);
                node.Children[word] = ReadNodeRecursive(reader);
            }

            return node;
        }
    }

    public static IEnumerable<string> RetrieveTrieFile(Stream fileStream, string prefix, Encoding? encoding = null)
    {
        var    trie   = new Patricia();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 4);
        int    bytes_read;
        bool   done = false;
        while (!done && (bytes_read = fileStream.Read(buffer, 0, buffer.Length)) > 0) {
            done = bytes_read < buffer.Length;
            trie.Deserialize(buffer.AsSpan(0, bytes_read), encoding);
            foreach (string word in trie.Retrieve(prefix)) {
                yield return word;
            }
        }

        ArrayPool<byte>.Shared.Return(buffer);
    }


    public static void AddWordTrieFile(Stream fileStream, string word, Encoding? encoding = null)
    {
        // Read the contents of the file into memory and construct the trie
        byte[] buffer = new byte[fileStream.Length];
        fileStream.Seek(0, SeekOrigin.Begin);
        fileStream.ReadExactly(buffer);
        var trie = new Patricia();
        trie.Deserialize(buffer, encoding);

        // Add the new word to the trie
        trie.Add(word);

        // Write the updated trie back to the file
        fileStream.Seek(0, SeekOrigin.Begin);
        var writer = new BinaryWriter(fileStream, encoding ?? Encoding.UTF8);
        writer.Write(trie.Serialize());
        writer.Flush();
    }

    public static string PrettyString(Stream fileStream)
    {
        fileStream.Seek(0, SeekOrigin.Begin);
        var trie = new Patricia();

        // Read the contents of the file into memory and construct the trie
        byte[] buffer = new byte[fileStream.Length];
        fileStream.Seek(0, SeekOrigin.Begin);
        fileStream.ReadExactly(buffer);
        trie.Deserialize(buffer);

        // Retrieve all words in the trie that match the prefix
        return trie.PrettyString();
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