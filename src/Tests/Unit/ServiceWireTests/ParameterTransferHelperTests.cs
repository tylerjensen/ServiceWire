#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWireTests
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using ServiceWire;

#endregion


namespace ServiceWireTests
{
    [TestClass]
    public class ParameterTransferHelperTests
    {
        #region Methods


        #region Public Methods

        [TestMethod]
        public void SimpleTypesTest()
        {
            var b=true;
            TestSingle(b); // bool), P
            byte y=0x32;
            TestSingle(y); // byte), P
            sbyte sb=-33;
            TestSingle(sb); // sbyte),
            var c='c';
            TestSingle(c); // char), P
            var d=1.34m;
            TestSingle(d); // decimal)
            var db=3.45;
            TestSingle(db); // double),
            var f=32.3f;
            TestSingle(f); // float),
            var i=34588;
            TestSingle(i); // int), Pa
            uint ui=34444;
            TestSingle(ui); // uint), P
            long l=3499887766;
            TestSingle(l); // long), P
            ulong ul=9876758765654;
            TestSingle(ul); // ulong),
            short st=234;
            TestSingle(st); // short),
            ushort ut=888;
            TestSingle(ut); // ushort),
            var str="hello";
            TestSingle(str); // string),
            var ty=str.GetType();
            TestSingle(ty); // Type), P
            var g=Guid.NewGuid();
            TestSingle(g); // Guid), P
            var dt=DateTime.Now;
            TestSingle(dt); // DateTime
        }

        [TestMethod]
        public void SimpleTypesMultipleTest()
        {
            var b=true;
            byte y=0x32;
            sbyte sb=-33;
            var c='c';
            var d=1.34m;
            TestMultiple(b,y,sb,c,d);

            var db=3.45;
            var f=32.3f;
            var i=34588;
            uint ui=34444;
            long l=3499887766;
            ulong ul=9876758765654;
            short st=234;
            ushort ut=888;
            TestMultiple(db,f,i,ui,l,ul,st,ut);

            var str="hello";
            var ty=str.GetType();
            var g=Guid.NewGuid();
            var dt=DateTime.Now;
            TestMultiple(str,ty,g,dt);
        }

        [TestMethod]
        public void ArrayOfSimpleTypesTest()
        {
            var bools=new[] {true,true,false,true,false};
            TestArraySimpleType(bools);

            var ys=new byte[] {0x32,0x33,0x42,0x52};
            TestArraySimpleType(ys);

            var sbs=new sbyte[] {-33,14,-12,-33,14,-12};
            TestArraySimpleType(sbs);

            var cs=new[] {'c','d','e','r'};
            TestArraySimpleType(cs);

            var ds=new[] {1.344m,1.374m,1.394m,1.314m};
            TestArraySimpleType(ds);

            var dbs=new[] {3.435,3.425,3.465,3.845,3.145};
            TestArraySimpleType(dbs);

            var fs=new[] {32.37f,362.3f,342.3f,32.23f,32.33f};
            TestArraySimpleType(fs);

            var iss=new[] {345288,541215,542315,542215,564215,544215};
            TestArraySimpleType(iss);

            var uis=new uint[] {34578444,45676456,452456456,452456456,424556456};
            TestArraySimpleType(uis);

            var ls=new long[] {34997766,99887766,34998866,34987766,34998877};
            TestArraySimpleType(ls);

            var uls=new ulong[] {987675765654,987758765654,987758765654,987675876654,987675875654};
            TestArraySimpleType(uls);

            var sts=new short[] {2343,2343,2334,2345,2434};
            TestArraySimpleType(sts);

            var uts=new ushort[] {8828,4244,4244,2444,4442,4244};
            TestArraySimpleType(uts);

            var strs=new[] {"hello","heallo","heldlo","hellao"};
            TestArraySimpleType(strs);

            var tys=new[] {strs.GetType(),uts.GetType(),sts.GetType()};
            TestArraySimpleType(tys);

            var gs=new[] {Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()};
            TestArraySimpleType(gs);

            var dts=new[] {DateTime.Now,DateTime.Now.AddDays(1),DateTime.Now.AddDays(2)};
            TestArraySimpleType(dts);
        }

        #endregion


        #region Private Methods

        private void TestArraySimpleType(object obj)
        {
            var result=RunInAndOut(obj);
            var arr=obj as Array;
            var rarr=result[0] as Array;
            Assert.IsNotNull(result);
            Assert.AreEqual(arr.Length,rarr.Length);
        }

        private void TestMultiple(params object[] obj)
        {
            var result=RunInAndOut(obj);
            Assert.IsNotNull(result);
            Assert.AreEqual(obj.Length,result.Length);
            for(var i=0;i<obj.Length;i++)
            {
                Assert.AreEqual(obj[i],result[i]);
            }
        }

        private void TestSingle(object obj)
        {
            var result=RunInAndOut(obj);
            Assert.IsNotNull(result);
            Assert.AreEqual(1,result.Length);
            Assert.AreEqual(obj,result[0]);
        }

        private object[] RunInAndOut(params object[] obj)
        {
            var pth=new ParameterTransferHelper();
            object[] result=null;
            using(var ms=new MemoryStream())
            {
                using(var writer=new BinaryWriter(ms))
                {
                    using(var reader=new BinaryReader(ms))
                    {
                        pth.SendParameters(false,0,writer,obj);
                        ms.Position=0;
                        result=pth.ReceiveParameters(reader);
                    }
                }
            }
            return result;
        }

        #endregion


        #endregion
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