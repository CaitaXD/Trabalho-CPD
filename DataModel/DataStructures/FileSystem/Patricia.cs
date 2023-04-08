using System.Buffers;
using System.Collections;
using System.Text;

namespace DataModel.DataStructures.FileSystem;

public class PatriciaStream : ITRie<string>
{
    readonly Patricia    _buffer       = new();
    bool                     _pendingWrite = false;
    readonly        Encoding _encoding     = Encoding.UTF8;
    public readonly Stream   BaseStream;

    public PatriciaStream(Stream baseStream)
    {
        BaseStream = baseStream;
    }

    public PatriciaStream(string path, FileMode mode)
    {
        BaseStream = File.Open(path, mode);
    }

    public IEnumerator<string> GetEnumerator()
    {
        return Retrieve("").GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Add(string key)
    {
        _pendingWrite = true;
        return _buffer.Add(key);
    }

    public void FLush()
    {
        if (_pendingWrite) {
            var patricia = new Patricia();
            patricia.ReadFromFile(BaseStream, _encoding);
            foreach (string key in _buffer) {
                patricia.Add(key); 
            }

            _buffer.Clear();
            _pendingWrite = false;
        }
    }

    public IEnumerable<string> Retrieve(string prefix)
    {
        FLush();
        
        return PatriciaExtension.RetrieveTrieFile(BaseStream, prefix, _encoding);
    }

    public string PrettyString()
    {
        return PatriciaExtension.PrettyString(BaseStream);
    }
}