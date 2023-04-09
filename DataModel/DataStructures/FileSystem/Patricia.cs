using System.Buffers;
using System.Collections;
using System.Numerics;
using System.Text;

namespace DataModel.DataStructures.FileSystem;

public class PatriciaStream : ITRie<string>, IDisposable
{
    public readonly Patricia Buffer        = new();
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
        return Buffer.Add(key);
    }

    public void FLush()
    {
        if (!_pendingWrite) return;
        Buffer.Write(BaseStream, _encoding);
        _pendingWrite = false;
    }

    public IEnumerable<string> Retrieve(string prefix)
    {
        BaseStream.Seek(0, SeekOrigin.Begin);
        
        var nodes = PatriciaExtension.ReadNodes(BaseStream, _encoding);

        var root = nodes.FirstOrDefault() ?? new Patricia.Node();
        
        foreach (string word in root.Retrieve(prefix)) {
            yield return word;
        }
    }
    public string PrettyString()
    {
        BaseStream.Seek(0, SeekOrigin.Begin);
        
        var nodes = PatriciaExtension.ReadNodes(BaseStream, _encoding);
        
        var root = nodes.FirstOrDefault() ?? new Patricia.Node();
        
        return root.PrettyString();
    }

    public IEnumerable<Patricia.Node> Traverse()
    {
        BaseStream.Seek(0, SeekOrigin.Begin);
        
        var nodes = PatriciaExtension.ReadNodes(BaseStream, _encoding);

        var patricia = new Patricia
        {
            Root = nodes.FirstOrDefault() ?? new Patricia.Node()
        };
        
        foreach (var node in patricia.Traverse()) {
            yield return node;
        }
    }
    public string Decode(int key)
    {
        BaseStream.Seek(0, SeekOrigin.Begin);
        
        var nodes = PatriciaExtension.ReadNodes(BaseStream, _encoding);

        var patricia = new Patricia
        {
            Root = nodes.FirstOrDefault() ?? new Patricia.Node()
        };

        return patricia.Decode(key);
    }
    public void Dispose()
    {
        BaseStream.Dispose();
        GC.SuppressFinalize(this);
        FLush();
    }
}