using System.Collections;

namespace DataModel.DataStructures;

public interface ITRie<TWord> : IEnumerable<TWord>
{
    int Add(TWord key);

    IEnumerable<TWord> Retrieve(string prefix);
    
    string PrettyString();
}