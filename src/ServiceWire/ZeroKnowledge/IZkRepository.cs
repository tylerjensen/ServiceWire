namespace ServiceWire.ZeroKnowledge
{
    public interface IZkRepository
    {
        ZkPasswordHash GetPasswordHashSet(string identity);
    }

    public class ZkNullRepository : IZkRepository
    {
        public ZkPasswordHash GetPasswordHashSet(string identity)
        {
            return null;
        }
    }
}
