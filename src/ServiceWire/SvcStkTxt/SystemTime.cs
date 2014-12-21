//
// https://github.com/ServiceStack/ServiceWire.SvcStkTxt
// ServiceWire.SvcStkTxt: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//   Damian Hickey (dhickey@gmail.com)
//
// Copyright 2012 ServiceStack Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;

namespace ServiceWire.SvcStkTxt
{
	public static class SystemTime
	{
		public static Func<DateTime> UtcDateTimeResolver;

		public static DateTime Now
		{
			get
			{
				var temp = UtcDateTimeResolver;
				return temp == null ? DateTime.Now : temp().ToLocalTime();
			}
		}

		public static DateTime UtcNow
		{
			get
			{
				var temp = UtcDateTimeResolver;
				return temp == null ? DateTime.UtcNow : temp();
			}
		}
	}
}
