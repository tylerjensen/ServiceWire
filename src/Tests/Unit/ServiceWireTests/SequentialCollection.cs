using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ServiceWireTests
{
    [CollectionDefinition("Sequential Collection", DisableParallelization = true)]
    public class SequentialCollection
    {
    }
}
