using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class ObjectSerializer
{
    
    public static void Encode(string path, object obj)
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        bf.Serialize(stream, obj);
        stream.Close();
    }

    public static object Decode(string path)
    {
        object toRet;
        BinaryFormatter bf = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        toRet = bf.Deserialize(stream);
        stream.Close();
        return toRet;
    }

}
public interface ISerializable
{
    void Export(string path);
    void Import(string path);

}
