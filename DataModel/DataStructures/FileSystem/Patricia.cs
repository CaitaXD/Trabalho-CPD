using System.Collections;
using System.Text;

namespace DataModel.DataStructures.FileSystem;

public class PatriciaStream : ITRie<string>
{
    readonly Encoding _encoding   = Encoding.UTF8;
    readonly Stream   _stream;

    public PatriciaStream(Stream stream)
    {
        _stream = stream;
    }

    public PatriciaStream(string path, FileMode mode)
    {
        _stream = File.Open(path, mode);
    }

    public IEnumerator<string> GetEnumerator()
    {
        return Retrieve("").GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(string key)
    {
        PatriciaExtension.AddWordTrieFile(_stream, key, _encoding);
    }

    public IEnumerable<string> Retrieve(string prefix)
    {
        return PatriciaExtension.RetrieveTrieFile(_stream, prefix, _encoding);
    }

    public string PrettyString()
    {
        return PatriciaExtension.PrettyString(_stream);
    }
}