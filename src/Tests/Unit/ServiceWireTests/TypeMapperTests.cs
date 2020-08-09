using ServiceWire;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ServiceWireTests
{
    public class TypeMapperTests
    {
        [Fact]
        public void CanMapUriTypes()
        {
            Type uriType = typeof(Uri);

            Type mapType = TypeMapper.GetType(uriType.FullName);

            Assert.NotNull(mapType);

            Assert.Equal(uriType.FullName, mapType.FullName);
        }
    }
}
