using System;
using System.IO;
using ServiceWire;
using Xunit;

namespace ServiceWireTests
{
    public class ParameterTransferHelperTests
    {
        [Fact]
        public void SimpleTypesTest()
        {
            bool b = true;
            TestSingle(b); // bool), P
            byte y = 0x32;
            TestSingle(y); // byte), P
            sbyte sb = -33;
            TestSingle(sb); // sbyte),
            char c = 'c';
            TestSingle(c); // char), P
            decimal d = 1.34m;
            TestSingle(d); // decimal)
            double db = 3.45;
            TestSingle(db); // double),
            float f = 32.3f;
            TestSingle(f); // float),
            int i = 34588;
            TestSingle(i); // int), Pa
            uint ui = 34444;
            TestSingle(ui); // uint), P
            long l = 3499887766;
            TestSingle(l); // long), P
            ulong ul = 9876758765654;
            TestSingle(ul); // ulong),
            short st = 234;
            TestSingle(st); // short),
            ushort ut = 888;
            TestSingle(ut); // ushort),
            string str = "hello";
            TestSingle(str); // string),
            Type ty = str.GetType();
            TestSingle(ty); // Type), P
            Guid g = Guid.NewGuid();
            TestSingle(g); // Guid), P
            DateTime dt = DateTime.Now;
            TestSingle(dt);  // DateTime
        }

        [Fact]
        public void SimpleTypesMultipleTest()
        {
            bool b = true;
            byte y = 0x32;
            sbyte sb = -33;
            char c = 'c';
            decimal d = 1.34m;
            TestMultiple(b, y, sb, c, d);

            double db = 3.45;
            float f = 32.3f;
            int i = 34588;
            uint ui = 34444;
            long l = 3499887766;
            ulong ul = 9876758765654;
            short st = 234;
            ushort ut = 888;
            TestMultiple(db, f, i, ui, l, ul, st, ut);

            string str = "hello";
            Type ty = str.GetType();
            Guid g = Guid.NewGuid();
            DateTime dt = DateTime.Now;
            TestMultiple(str, ty, g, dt);
        }

        [Fact]
        public void ArrayOfSimpleTypesTest()
        {
            var bools = new bool[] { true, true, false, true, false };
            TestArraySimpleType(bools);

            var ys = new byte[] { 0x32, 0x33, 0x42, 0x52 };
            TestArraySimpleType(ys);

            var sbs = new sbyte[] { -33, 14, -12, -33, 14, -12 };
            TestArraySimpleType(sbs);

            var cs = new char[] { 'c', 'd', 'e', 'r' };
            TestArraySimpleType(cs);

            var ds = new decimal[] { 1.344m, 1.374m, 1.394m, 1.314m };
            TestArraySimpleType(ds);

            var dbs = new double[] { 3.435, 3.425, 3.465, 3.845, 3.145 };
            TestArraySimpleType(dbs);

            var fs = new float[] { 32.37f, 362.3f, 342.3f, 32.23f, 32.33f };
            TestArraySimpleType(fs);

            var iss = new int[] { 345288, 541215, 542315, 542215, 564215, 544215 };
            TestArraySimpleType(iss);

            var uis = new uint[] { 34578444, 45676456, 452456456, 452456456, 424556456 };
            TestArraySimpleType(uis);

            var ls = new long[] { 34997766, 99887766, 34998866, 34987766, 34998877};
            TestArraySimpleType(ls);

            var uls = new ulong[] { 987675765654, 987758765654, 987758765654, 987675876654, 987675875654 };
            TestArraySimpleType(uls);

            var sts = new short[] { 2343, 2343, 2334, 2345, 2434 };
            TestArraySimpleType(sts);

            var uts = new ushort[] { 8828, 4244, 4244, 2444, 4442, 4244 };
            TestArraySimpleType(uts);

            var strs = new string[] { "hello", "heallo", "heldlo", "hellao" };
            TestArraySimpleType(strs);
            
            var tys = new Type[] { strs.GetType(), uts.GetType(), sts.GetType() };
            TestArraySimpleType(tys);

            var gs = new Guid[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            TestArraySimpleType(gs);

            var dts = new DateTime[] { DateTime.Now, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2) };
            TestArraySimpleType(dts);
        }

        private void TestArraySimpleType(object obj)
        {
            var result = RunInAndOut(obj);
            var arr = obj as Array;
            var rarr = result[0] as Array;
            Assert.NotNull(result);
            Assert.Equal(arr.Length, rarr.Length);
        }

        private void TestMultiple(params object[] obj)
        {
            var result = RunInAndOut(obj);
            Assert.NotNull(result);
            Assert.Equal(obj.Length, result.Length);
            for (int i = 0; i < obj.Length; i++)
            {
                Assert.Equal(obj[i], result[i]);
            }
        }

        private void TestSingle(object obj)
        {
            var result = RunInAndOut(obj);
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(obj, result[0]);
        }

        private object[] RunInAndOut(params object[] obj)
        {
            var pth = new ParameterTransferHelper();
            object[] result = null;
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            using (var reader = new BinaryReader(ms))
            {
                pth.SendParameters(false, 0, writer, obj);
                ms.Position = 0;
                result = pth.ReceiveParameters(reader);
            }
            return result;
        }
    }
}


/*
                _parameterTypes.Add(typeof(bool), ParameterTypes.Bool);
                _parameterTypes.Add(typeof(byte), ParameterTypes.Byte);
                _parameterTypes.Add(typeof(sbyte), ParameterTypes.SByte);
                _parameterTypes.Add(typeof(char), ParameterTypes.Char);
                _parameterTypes.Add(typeof(decimal), ParameterTypes.Decimal);
                _parameterTypes.Add(typeof(double), ParameterTypes.Double);
                _parameterTypes.Add(typeof(float), ParameterTypes.Float);
                _parameterTypes.Add(typeof(int), ParameterTypes.Int);
                _parameterTypes.Add(typeof(uint), ParameterTypes.UInt);
                _parameterTypes.Add(typeof(long), ParameterTypes.Long);
                _parameterTypes.Add(typeof(ulong), ParameterTypes.ULong);
                _parameterTypes.Add(typeof(short), ParameterTypes.Short);
                _parameterTypes.Add(typeof(ushort), ParameterTypes.UShort);
                _parameterTypes.Add(typeof(string), ParameterTypes.String);
                _parameterTypes.Add(typeof(byte[]), ParameterTypes.ByteArray);
                _parameterTypes.Add(typeof(char[]), ParameterTypes.CharArray);
                _parameterTypes.Add(typeof(Type), ParameterTypes.Type);
                _parameterTypes.Add(typeof(Guid), ParameterTypes.Guid);
                _parameterTypes.Add(typeof(DateTime), ParameterTypes.DateTime);

                _parameterTypes.Add(typeof(bool[]), ParameterTypes.ArrayBool);
                _parameterTypes.Add(typeof(sbyte[]), ParameterTypes.ArraySByte);
                _parameterTypes.Add(typeof(decimal[]), ParameterTypes.ArrayDecimal);
                _parameterTypes.Add(typeof(double[]), ParameterTypes.ArrayDouble);
                _parameterTypes.Add(typeof(float[]), ParameterTypes.ArrayFloat);
                _parameterTypes.Add(typeof(int[]), ParameterTypes.ArrayInt);
                _parameterTypes.Add(typeof(uint[]), ParameterTypes.ArrayUInt);
                _parameterTypes.Add(typeof(long[]), ParameterTypes.ArrayLong);
                _parameterTypes.Add(typeof(ulong[]), ParameterTypes.ArrayULong);
                _parameterTypes.Add(typeof(short[]), ParameterTypes.ArrayShort);
                _parameterTypes.Add(typeof(ushort[]), ParameterTypes.ArrayUShort);
                _parameterTypes.Add(typeof(string[]), ParameterTypes.ArrayString);
                _parameterTypes.Add(typeof(Type[]), ParameterTypes.ArrayType);
                _parameterTypes.Add(typeof(Guid[]), ParameterTypes.ArrayGuid);
                _parameterTypes.Add(typeof(DateTime[]), ParameterTypes.ArrayDateTime);
 
 */