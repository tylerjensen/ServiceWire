#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


namespace ServiceWire.ZeroKnowledge
{
    public interface IZkRepository
    {
        #region Methods


        #region Public Methods

        ZkPasswordHash GetPasswordHashSet(string username);

        #endregion


        #endregion
    }

    public class ZkNullRepository:IZkRepository
    {
        #region Methods


        #region Public Methods

        public ZkPasswordHash GetPasswordHashSet(string username)
        {
            return null;
        }

        #endregion


        #endregion
    }
}