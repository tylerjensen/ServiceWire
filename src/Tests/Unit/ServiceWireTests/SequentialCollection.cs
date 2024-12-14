using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ServiceWireTests
{
    [CollectionDefinition("Sequential Collection Tcp", DisableParallelization = true)]
    public class SequentialCollectionTcp
    {
    }

    [CollectionDefinition("Sequential Collection TcpZk", DisableParallelization = true)]
    public class SequentialCollectionTcpZk
    {
    }
}
