namespace ServiceWire
{
    public interface ISerializer
    {
        byte[] Serialize<T>(T obj);
        byte[] Serialize(object obj, string typeConfigName);
        T Deserialize<T>(byte[] bytes);
        object Deserialize(byte[] bytes, string typeConfigName);
    }
}