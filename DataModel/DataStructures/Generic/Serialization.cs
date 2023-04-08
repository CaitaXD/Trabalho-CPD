using System.Text;

namespace DataModel.DataStructures.Generic;

public static partial class Serialization
{
    public static Span<byte> Serialize(this PatriciaTrie<char> patriciaTrie, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        SerializeNodeRecursive(patriciaTrie.Root, writer, encoding);

        byte[] bytes = stream.ToArray();

        return bytes;

        static void SerializeNodeRecursive(PatriciaTrie<char>.Node node, BinaryWriter writer, Encoding? encoding = null)
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
    }

    public static void Deserialize(this PatriciaTrie<char> patriciaTrie, Span<byte> bytes,
        Encoding?                                                      encoding = null)
    {
        encoding ??= Encoding.UTF8;

        using var stream = new MemoryStream(bytes.ToArray());
        using var reader = new BinaryReader(stream);

        patriciaTrie.Root = DeserializeNodeRecursive(reader, encoding);

        static PatriciaTrie<char>.Node DeserializeNodeRecursive(BinaryReader reader, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;


            var node = new PatriciaTrie<char>.Node
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

    public static void WriteToFile(this PatriciaTrie<char> patriciaTrie,           string    filePath,
        FileMode                                           mode = FileMode.Create, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        using var writer = new BinaryWriter(File.Open(filePath, mode));
        WriteNodeRecursive(patriciaTrie.Root, writer, encoding);

        static void WriteNodeRecursive(PatriciaTrie<char>.Node node, BinaryWriter writer, Encoding? encoding = null)
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

    public static void ReadFromFile(this PatriciaTrie<char> patriciaTrie, string filePath, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        using var reader = new BinaryReader(File.OpenRead(filePath));
        patriciaTrie.Root = ReadNodeRecursive(reader);

        static PatriciaTrie<char>.Node ReadNodeRecursive(BinaryReader reader, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;

            var node = new PatriciaTrie<char>.Node
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
}