using ServiceWire;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace ServiceWireTests
{
	public class NetExtensionsTest
	{
		// C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Data.Common.dll
		
		// C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.2\System.Data.dll

		[Fact]
		public void TypeConfigName()
		{
			Type[] types = { typeof(Int32), typeof(CustomType), typeof(DataSet) };

			foreach (var t in types)
			{
				var configName = t.ToConfigName();

				var t2 = configName.ToType();

				Assert.Equal(t, t2);
			}
		}
	}

	class CustomType
	{

	}
}
