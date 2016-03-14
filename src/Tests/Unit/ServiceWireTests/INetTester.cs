#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWireTests
// On 2016 03 14 04:36

#endregion


#region Usings

using System.Collections.Generic;

#endregion


namespace ServiceWireTests
{
    public interface INetTester
    {
        #region Methods


        #region Public Methods

        int Min(int a,int b);

        Dictionary<int,int> Range(int start,int count);

        #endregion


        #endregion
    }
}