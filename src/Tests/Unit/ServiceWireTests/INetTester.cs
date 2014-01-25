using System.Collections.Generic;

namespace ServiceWireTests
{
    public interface INetTester
    {
        int Min(int a, int b);
        Dictionary<int, int> Range(int start, int count);
    }
}