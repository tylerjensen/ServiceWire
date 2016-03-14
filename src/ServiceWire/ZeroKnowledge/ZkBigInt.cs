#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

#endregion


namespace ServiceWire.ZeroKnowledge
{
    #region Usings

    using DType = UInt32;

    #endregion


    // This could be UInt32, UInt16 or Byte; not UInt64.


    #region DigitsArray

    internal class DigitsArray
    {
        #region Constractor

        internal DigitsArray(int size)
        {
            Allocate(size,0);
        }

        internal DigitsArray(int size,int used)
        {
            Allocate(size,used);
        }

        internal DigitsArray(uint[] copyFrom)
        {
            Allocate(copyFrom.Length);
            CopyFrom(copyFrom,0,0,copyFrom.Length);
            ResetDataUsed();
        }

        internal DigitsArray(DigitsArray copyFrom)
        {
            Allocate(copyFrom.Count,copyFrom.DataUsed);
            Array.Copy(copyFrom.m_data,0,m_data,0,copyFrom.Count);
        }

        static DigitsArray()
        {
            unchecked
            {
                AllBits=~((uint)0);
                HiBitSet=((uint)1)<<(DataSizeBits)-1;
            }
        }

        #endregion


        #region Fields

        private uint[] m_data;

        internal static readonly uint AllBits; // = ~((DType)0);
        internal static readonly uint HiBitSet; // = 0x80000000;

        #endregion


        #region  Proporties

        internal int DataUsed { get; set; }

        #endregion


        #region Methods


        #region Public Methods

        public void Allocate(int size)
        {
            Allocate(size,0);
        }

        public void Allocate(int size,int used)
        {
            m_data=new uint[size+1];
            DataUsed=used;
        }

        #endregion


        #region Other Methods

        internal void CopyFrom(uint[] source,int sourceOffset,int offset,int length)
        {
            Array.Copy(source,sourceOffset,m_data,0,length);
        }

        internal void CopyTo(uint[] array,int offset,int length)
        {
            Array.Copy(m_data,0,array,offset,length);
        }

        internal void ResetDataUsed()
        {
            DataUsed=m_data.Length;
            if(IsNegative)
            {
                while(DataUsed>1&&m_data[DataUsed-1]==AllBits)
                {
                    --DataUsed;
                }
                DataUsed++;
            } else
            {
                while(DataUsed>1&&m_data[DataUsed-1]==0)
                {
                    --DataUsed;
                }
                if(DataUsed==0)
                {
                    DataUsed=1;
                }
            }
        }

        internal int ShiftRight(int shiftCount)
        {
            return ShiftRight(m_data,shiftCount);
        }

        internal static int ShiftRight(uint[] buffer,int shiftCount)
        {
            var shiftAmount=DataSizeBits;
            var invShift=0;
            var bufLen=buffer.Length;

            while(bufLen>1&&buffer[bufLen-1]==0)
            {
                bufLen--;
            }

            for(var count=shiftCount;count>0;count-=shiftAmount)
            {
                if(count<shiftAmount)
                {
                    shiftAmount=count;
                    invShift=DataSizeBits-shiftAmount;
                }

                ulong carry=0;
                for(var i=bufLen-1;i>=0;i--)
                {
                    var val=((ulong)buffer[i])>>shiftAmount;
                    val|=carry;

                    carry=((ulong)buffer[i])<<invShift;
                    buffer[i]=(uint)(val);
                }
            }

            while(bufLen>1&&buffer[bufLen-1]==0)
            {
                bufLen--;
            }

            return bufLen;
        }

        internal int ShiftLeft(int shiftCount)
        {
            return ShiftLeft(m_data,shiftCount);
        }

        internal static int ShiftLeft(uint[] buffer,int shiftCount)
        {
            var shiftAmount=DataSizeBits;
            var bufLen=buffer.Length;

            while(bufLen>1&&buffer[bufLen-1]==0)
            {
                bufLen--;
            }

            for(var count=shiftCount;count>0;count-=shiftAmount)
            {
                if(count<shiftAmount)
                {
                    shiftAmount=count;
                }

                ulong carry=0;
                for(var i=0;i<bufLen;i++)
                {
                    var val=((ulong)buffer[i])<<shiftAmount;
                    val|=carry;

                    buffer[i]=(uint)(val&AllBits);
                    carry=(val>>DataSizeBits);
                }

                if(carry!=0)
                {
                    if(bufLen+1<=buffer.Length)
                    {
                        buffer[bufLen]=(uint)carry;
                        bufLen++;
                        carry=0;
                    } else
                    {
                        throw new OverflowException();
                    }
                }
            }
            return bufLen;
        }

        internal int ShiftLeftWithoutOverflow(int shiftCount)
        {
            var temporary=new List<uint>(m_data);
            var shiftAmount=DataSizeBits;

            for(var count=shiftCount;count>0;count-=shiftAmount)
            {
                if(count<shiftAmount)
                {
                    shiftAmount=count;
                }

                ulong carry=0;
                for(var i=0;i<temporary.Count;i++)
                {
                    var val=((ulong)temporary[i])<<shiftAmount;
                    val|=carry;

                    temporary[i]=(uint)(val&AllBits);
                    carry=(val>>DataSizeBits);
                }

                if(carry!=0)
                {
                    temporary.Add(0);
                    temporary[temporary.Count-1]=(uint)carry;
                }
            }
            m_data=new uint[temporary.Count];
            temporary.CopyTo(m_data);
            return m_data.Length;
        }

        #endregion


        #endregion


        #region  Others

        internal static int DataSizeOf
        {
            get { return sizeof(uint); }
        }

        internal static int DataSizeBits
        {
            get { return sizeof(uint)*8; }
        }

        internal uint this[int index]
        {
            get
            {
                if(index<DataUsed)
                {
                    return m_data[index];
                }
                return (IsNegative ? AllBits : 0);
            }
            set { m_data[index]=value; }
        }

        internal int Count
        {
            get { return m_data.Length; }
        }

        internal bool IsZero
        {
            get { return DataUsed==0||(DataUsed==1&&m_data[0]==0); }
        }

        internal bool IsNegative
        {
            get { return (m_data[m_data.Length-1]&HiBitSet)==HiBitSet; }
        }

        #endregion
    }

    #endregion


    /// <summary>
    ///     Represents a integer of abitrary length.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A BigInteger object is immutable like System.String. The object can not be modifed, and new BigInteger objects
    ///         are
    ///         created by using the operations of existing BigInteger objects.
    ///     </para>
    ///     <para>
    ///         Internally a BigInteger object is an array of ? that is represents the digits of the n-place integer. Negative
    ///         BigIntegers
    ///         are stored internally as 1's complements, thus every BigInteger object contains 1 or more padding elements to
    ///         hold the sign.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///  public class MainProgram
    ///  {
    /// 		[STAThread]
    /// 		public static void Main(string[] args)
    /// 		{
    /// 			BigInteger a = new BigInteger(25);
    /// 			a = a + 100;
    /// 			
    /// 			BigInteger b = new BigInteger("139435810094598308945890230913");
    /// 			
    /// 			BigInteger c = b / a;
    /// 			BigInteger d = b % a;
    /// 			
    /// 			BigInteger e = (c * a) + d;
    /// 			if (e != b)
    /// 			{
    /// 				Console.WriteLine("Can never be true.");
    /// 			}
    /// 		}
    /// 	</code>
    /// </example>
    public class BigInteger
    {
        #region Fields

        private DigitsArray m_digits;

        #endregion


        #region Methods


        #region Public Methods

        /// <summary>
        ///     Converts the numeric value of this instance to its equivalent string representation in specified base.
        /// </summary>
        /// <param name="radix">Int radix between 2 and 36</param>
        /// <returns>A string.</returns>
        public string ToString(int radix)
        {
            if(radix<2||radix>36)
            {
                throw new ArgumentOutOfRangeException("radix");
            }

            if(IsZero)
            {
                return "0";
            }

            var a=this;
            var negative=a.IsNegative;
            a=Abs(this);

            BigInteger quotient;
            BigInteger remainder;
            var biRadix=new BigInteger(radix);

            const string charSet="0123456789abcdefghijklmnopqrstuvwxyz";
            var al=new ArrayList();
            while(a.m_digits.DataUsed>1||(a.m_digits.DataUsed==1&&a.m_digits[0]!=0))
            {
                Divide(a,biRadix,out quotient,out remainder);
                al.Insert(0,charSet[(int)remainder.m_digits[0]]);
                a=quotient;
            }

            var result=new string((char[])al.ToArray(typeof(char)));
            if(radix==10&&negative)
            {
                return "-"+result;
            }

            return result;
        }

        /// <summary>
        ///     Returns string in hexidecimal of the internal digit representation.
        /// </summary>
        /// <remarks>
        ///     This is not the same as ToString(16). This method does not return the sign, but instead
        ///     dumps the digits array into a string representation in base 16.
        /// </remarks>
        /// <returns>A string in base 16.</returns>
        public string ToHexString()
        {
            var sb=new StringBuilder();
            sb.AppendFormat("{0:X}",m_digits[m_digits.DataUsed-1]);

            var f="{0:X"+(2*DigitsArray.DataSizeOf)+"}";
            for(var i=m_digits.DataUsed-2;i>=0;i--)
            {
                sb.AppendFormat(f,m_digits[i]);
            }

            return sb.ToString();
        }

        /// <summary>
        ///     Returns BigInteger as System.Int16 if possible.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Int value of BigInteger</returns>
        /// <exception cref="System.Exception">When BigInteger is too large to fit into System.Int16</exception>
        public static int ToInt16(BigInteger value)
        {
            if(ReferenceEquals(value,null))
            {
                throw new ArgumentNullException("value");
            }
            return short.Parse(value.ToString(),NumberStyles.Integer,CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///     Returns BigInteger as System.UInt16 if possible.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception">When BigInteger is too large to fit into System.UInt16</exception>
        public static uint ToUInt16(BigInteger value)
        {
            if(ReferenceEquals(value,null))
            {
                throw new ArgumentNullException("value");
            }
            return ushort.Parse(value.ToString(),NumberStyles.Integer,CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///     Returns BigInteger as System.Int32 if possible.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception">When BigInteger is too large to fit into System.Int32</exception>
        public static int ToInt32(BigInteger value)
        {
            if(ReferenceEquals(value,null))
            {
                throw new ArgumentNullException("value");
            }
            return int.Parse(value.ToString(),NumberStyles.Integer,CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///     Returns BigInteger as System.UInt32 if possible.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception">When BigInteger is too large to fit into System.UInt32</exception>
        public static uint ToUInt32(BigInteger value)
        {
            if(ReferenceEquals(value,null))
            {
                throw new ArgumentNullException("value");
            }
            return uint.Parse(value.ToString(),NumberStyles.Integer,CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///     Returns BigInteger as System.Int64 if possible.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception">When BigInteger is too large to fit into System.Int64</exception>
        public static long ToInt64(BigInteger value)
        {
            if(ReferenceEquals(value,null))
            {
                throw new ArgumentNullException("value");
            }
            return long.Parse(value.ToString(),NumberStyles.Integer,CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///     Returns BigInteger as System.UInt64 if possible.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception">When BigInteger is too large to fit into System.UInt64</exception>
        public static ulong ToUInt64(BigInteger value)
        {
            if(ReferenceEquals(value,null))
            {
                throw new ArgumentNullException("value");
            }
            return ulong.Parse(value.ToString(),NumberStyles.Integer,CultureInfo.CurrentCulture);
        }

        public static byte[] ToByteArray(BigInteger value)
        {
            if(ReferenceEquals(value,null))
            {
                throw new ArgumentNullException("value");
            }
            var arr=new uint[value.m_digits.DataUsed];
            value.m_digits.CopyTo(arr,0,value.m_digits.DataUsed);
            var len=Buffer.ByteLength(arr);
            var buf=new byte[len];
            Buffer.BlockCopy(arr,0,buf,0,len);
            Array.Reverse(buf);
            return buf;
        }

        #endregion


        #endregion


        #region Constructors

        /// <summary>
        ///     Create a BigInteger with an integer value of 0.
        /// </summary>
        public BigInteger()
        {
            m_digits=new DigitsArray(1,1);
        }

        /// <summary>
        ///     Creates a BigInteger with the value of the operand.
        /// </summary>
        /// <param name="number">A long.</param>
        public BigInteger(long number)
        {
            m_digits=new DigitsArray((8/DigitsArray.DataSizeOf)+1,0);
            while(number!=0&&m_digits.DataUsed<m_digits.Count)
            {
                m_digits[m_digits.DataUsed]=(uint)(number&DigitsArray.AllBits);
                number>>=DigitsArray.DataSizeBits;
                m_digits.DataUsed++;
            }
            m_digits.ResetDataUsed();
        }

        /// <summary>
        ///     Creates a BigInteger with the value of the operand. Can never be negative.
        /// </summary>
        /// <param name="number">A unsigned long.</param>
        public BigInteger(ulong number)
        {
            m_digits=new DigitsArray((8/DigitsArray.DataSizeOf)+1,0);
            while(number!=0&&m_digits.DataUsed<m_digits.Count)
            {
                m_digits[m_digits.DataUsed]=(uint)(number&DigitsArray.AllBits);
                number>>=DigitsArray.DataSizeBits;
                m_digits.DataUsed++;
            }
            m_digits.ResetDataUsed();
        }

        /// <summary>
        ///     Creates a BigInteger initialized from the byte array.
        /// </summary>
        /// <param name="array"></param>
        public BigInteger(byte[] array)
        {
            ConstructFrom(array,0,array.Length);
        }

        /// <summary>
        ///     Creates a BigInteger initialized from the byte array ending at <paramref name="length" />.
        /// </summary>
        /// <param name="array">A byte array.</param>
        /// <param name="length">Int number of bytes to use.</param>
        public BigInteger(byte[] array,int length)
        {
            ConstructFrom(array,0,length);
        }

        /// <summary>
        ///     Creates a BigInteger initialized from <paramref name="length" /> bytes starting at <paramref name="offset" />.
        /// </summary>
        /// <param name="array">A byte array.</param>
        /// <param name="offset">Int offset into the <paramref name="array" />.</param>
        /// <param name="length">Int number of bytes.</param>
        public BigInteger(byte[] array,int offset,int length)
        {
            ConstructFrom(array,offset,length);
        }

        private void ConstructFrom(byte[] array,int offset,int length)
        {
            if(array==null)
            {
                throw new ArgumentNullException("array");
            }
            if(offset>array.Length||length>array.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if(length>array.Length||(offset+length)>array.Length)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            var estSize=length/4;
            var leftOver=length&3;
            if(leftOver!=0)
            {
                ++estSize;
            }

            m_digits=new DigitsArray(estSize+1,0); // alloc one extra since we can't init -'s from here.

            for(int i=offset+length-1,j=0;(i-offset)>=3;i-=4, j++)
            {
                m_digits[j]=(uint)((array[i-3]<<24)+(array[i-2]<<16)+(array[i-1]<<8)+array[i]);
                m_digits.DataUsed++;
            }

            uint accumulator=0;
            for(var i=leftOver;i>0;i--)
            {
                uint digit=array[offset+leftOver-i];
                digit=(digit<<((i-1)*8));
                accumulator|=digit;
            }
            m_digits[m_digits.DataUsed]=accumulator;

            m_digits.ResetDataUsed();
        }

        /// <summary>
        ///     Creates a BigInteger in base-10 from the parameter.
        /// </summary>
        /// <remarks>
        ///     The new BigInteger is negative if the <paramref name="digits" /> has a leading - (minus).
        /// </remarks>
        /// <param name="digits">A string</param>
        public BigInteger(string digits)
        {
            Construct(digits,10);
        }

        /// <summary>
        ///     Creates a BigInteger in base and value from the parameters.
        /// </summary>
        /// <remarks>
        ///     The new BigInteger is negative if the <paramref name="digits" /> has a leading - (minus).
        /// </remarks>
        /// <param name="digits">A string</param>
        /// <param name="radix">A int between 2 and 36.</param>
        public BigInteger(string digits,int radix)
        {
            Construct(digits,radix);
        }

        private void Construct(string digits,int radix)
        {
            if(digits==null)
            {
                throw new ArgumentNullException("digits");
            }

            var multiplier=new BigInteger(1);
            var result=new BigInteger();
            digits=digits.ToUpper(CultureInfo.CurrentCulture).Trim();

            var nDigits=(digits[0]=='-' ? 1 : 0);

            for(var idx=digits.Length-1;idx>=nDigits;idx--)
            {
                int d=digits[idx];
                if(d>='0'&&d<='9')
                {
                    d-='0';
                } else if(d>='A'&&d<='Z')
                {
                    d=(d-'A')+10;
                } else
                {
                    throw new ArgumentOutOfRangeException("digits");
                }

                if(d>=radix)
                {
                    throw new ArgumentOutOfRangeException("digits");
                }
                result+=(multiplier*d);
                multiplier*=radix;
            }

            if(digits[0]=='-')
            {
                result=-result;
            }

            m_digits=result.m_digits;
        }

        /// <summary>
        ///     Copy constructor, doesn't copy the digits parameter, assumes <code>this</code> owns the DigitsArray.
        /// </summary>
        /// <remarks>The <paramef name="digits" /> parameter is saved and reset.</remarks>
        /// <param name="digits"></param>
        private BigInteger(DigitsArray digits)
        {
            digits.ResetDataUsed();
            m_digits=digits;
        }

        #endregion


        #region Public Properties

        /// <summary>
        ///     A bool value that is true when the BigInteger is negative (less than zero).
        /// </summary>
        /// <value>
        ///     A bool value that is true when the BigInteger is negative (less than zero).
        /// </value>
        public bool IsNegative
        {
            get { return m_digits.IsNegative; }
        }

        /// <summary>
        ///     A bool value that is true when the BigInteger is exactly zero.
        /// </summary>
        /// <value>
        ///     A bool value that is true when the BigInteger is exactly zero.
        /// </value>
        public bool IsZero
        {
            get { return m_digits.IsZero; }
        }

        #endregion


        #region Implicit Type Operators Overloads

        /// <summary>
        ///     Creates a BigInteger from a long.
        /// </summary>
        /// <param name="value">A long.</param>
        /// <returns>A BigInteger initialzed by <paramref name="value" />.</returns>
        public static implicit operator BigInteger(long value)
        {
            return (new BigInteger(value));
        }

        /// <summary>
        ///     Creates a BigInteger from a ulong.
        /// </summary>
        /// <param name="value">A ulong.</param>
        /// <returns>A BigInteger initialzed by <paramref name="value" />.</returns>
        public static implicit operator BigInteger(ulong value)
        {
            return (new BigInteger(value));
        }

        /// <summary>
        ///     Creates a BigInteger from a int.
        /// </summary>
        /// <param name="value">A int.</param>
        /// <returns>A BigInteger initialzed by <paramref name="value" />.</returns>
        public static implicit operator BigInteger(int value)
        {
            return (new BigInteger(value));
        }

        /// <summary>
        ///     Creates a BigInteger from a uint.
        /// </summary>
        /// <param name="value">A uint.</param>
        /// <returns>A BigInteger initialzed by <paramref name="value" />.</returns>
        public static implicit operator BigInteger(uint value)
        {
            return (new BigInteger((ulong)value));
        }

        #endregion


        #region Addition and Subtraction Operator Overloads

        /// <summary>
        ///     Adds two BigIntegers and returns a new BigInteger that represents the sum.
        /// </summary>
        /// <param name="leftSide">A BigInteger</param>
        /// <param name="rightSide">A BigInteger</param>
        /// <returns>The BigInteger result of adding <paramref name="leftSide" /> and <paramref name="rightSide" />.</returns>
        public static BigInteger operator +(BigInteger leftSide,BigInteger rightSide)
        {
            var size=Math.Max(leftSide.m_digits.DataUsed,rightSide.m_digits.DataUsed);
            var da=new DigitsArray(size+1);

            long carry=0;
            for(var i=0;i<da.Count;i++)
            {
                var sum=leftSide.m_digits[i]+(long)rightSide.m_digits[i]+carry;
                carry=sum>>DigitsArray.DataSizeBits;
                da[i]=(uint)(sum&DigitsArray.AllBits);
            }

            return new BigInteger(da);
        }

        /// <summary>
        ///     Adds two BigIntegers and returns a new BigInteger that represents the sum.
        /// </summary>
        /// <param name="leftSide">A BigInteger</param>
        /// <param name="rightSide">A BigInteger</param>
        /// <returns>The BigInteger result of adding <paramref name="leftSide" /> and <paramref name="rightSide" />.</returns>
        public static BigInteger Add(BigInteger leftSide,BigInteger rightSide)
        {
            return leftSide-rightSide;
        }

        /// <summary>
        ///     Increments the BigInteger operand by 1.
        /// </summary>
        /// <param name="leftSide">The BigInteger operand.</param>
        /// <returns>The value of <paramref name="leftSide" /> incremented by 1.</returns>
        public static BigInteger operator ++(BigInteger leftSide)
        {
            return (leftSide+1);
        }

        /// <summary>
        ///     Increments the BigInteger operand by 1.
        /// </summary>
        /// <param name="leftSide">The BigInteger operand.</param>
        /// <returns>The value of <paramref name="leftSide" /> incremented by 1.</returns>
        public static BigInteger Increment(BigInteger leftSide)
        {
            return (leftSide+1);
        }

        /// <summary>
        ///     Substracts two BigIntegers and returns a new BigInteger that represents the sum.
        /// </summary>
        /// <param name="leftSide">A BigInteger</param>
        /// <param name="rightSide">A BigInteger</param>
        /// <returns>The BigInteger result of substracting <paramref name="leftSide" /> and <paramref name="rightSide" />.</returns>
        public static BigInteger operator -(BigInteger leftSide,BigInteger rightSide)
        {
            var size=Math.Max(leftSide.m_digits.DataUsed,rightSide.m_digits.DataUsed)+1;
            var da=new DigitsArray(size);

            long carry=0;
            for(var i=0;i<da.Count;i++)
            {
                var diff=leftSide.m_digits[i]-(long)rightSide.m_digits[i]-carry;
                da[i]=(uint)(diff&DigitsArray.AllBits);
                da.DataUsed++;
                carry=((diff<0) ? 1 : 0);
            }
            return new BigInteger(da);
        }

        /// <summary>
        ///     Substracts two BigIntegers and returns a new BigInteger that represents the sum.
        /// </summary>
        /// <param name="leftSide">A BigInteger</param>
        /// <param name="rightSide">A BigInteger</param>
        /// <returns>The BigInteger result of substracting <paramref name="leftSide" /> and <paramref name="rightSide" />.</returns>
        public static BigInteger Subtract(BigInteger leftSide,BigInteger rightSide)
        {
            return leftSide-rightSide;
        }

        /// <summary>
        ///     Decrements the BigInteger operand by 1.
        /// </summary>
        /// <param name="leftSide">The BigInteger operand.</param>
        /// <returns>The value of the <paramref name="leftSide" /> decremented by 1.</returns>
        public static BigInteger operator --(BigInteger leftSide)
        {
            return (leftSide-1);
        }

        /// <summary>
        ///     Decrements the BigInteger operand by 1.
        /// </summary>
        /// <param name="leftSide">The BigInteger operand.</param>
        /// <returns>The value of the <paramref name="leftSide" /> decremented by 1.</returns>
        public static BigInteger Decrement(BigInteger leftSide)
        {
            return (leftSide-1);
        }

        #endregion


        #region Negate Operator Overload

        /// <summary>
        ///     Negates the BigInteger, that is, if the BigInteger is negative return a positive BigInteger, and if the
        ///     BigInteger is negative return the postive.
        /// </summary>
        /// <param name="leftSide">A BigInteger operand.</param>
        /// <returns>The value of the <paramref name="this" /> negated.</returns>
        public static BigInteger operator -(BigInteger leftSide)
        {
            if(ReferenceEquals(leftSide,null))
            {
                throw new ArgumentNullException("leftSide");
            }

            if(leftSide.IsZero)
            {
                return new BigInteger(0);
            }

            var da=new DigitsArray(leftSide.m_digits.DataUsed+1,leftSide.m_digits.DataUsed+1);

            for(var i=0;i<da.Count;i++)
            {
                da[i]=~(leftSide.m_digits[i]);
            }

            // add one to result (1's complement + 1)
            var carry=true;
            var index=0;
            while(carry&&index<da.Count)
            {
                var val=(long)da[index]+1;
                da[index]=(uint)(val&DigitsArray.AllBits);
                carry=((val>>DigitsArray.DataSizeBits)>0);
                index++;
            }

            return new BigInteger(da);
        }

        /// <summary>
        ///     Negates the BigInteger, that is, if the BigInteger is negative return a positive BigInteger, and if the
        ///     BigInteger is negative return the postive.
        /// </summary>
        /// <returns>The value of the <paramref name="this" /> negated.</returns>
        public BigInteger Negate()
        {
            return -this;
        }

        /// <summary>
        ///     Creates a BigInteger absolute value of the operand.
        /// </summary>
        /// <param name="leftSide">A BigInteger.</param>
        /// <returns>A BigInteger that represents the absolute value of <paramref name="leftSide" />.</returns>
        public static BigInteger Abs(BigInteger leftSide)
        {
            if(ReferenceEquals(leftSide,null))
            {
                throw new ArgumentNullException("leftSide");
            }
            if(leftSide.IsNegative)
            {
                return -leftSide;
            }
            return leftSide;
        }

        #endregion


        #region Multiplication, Division and Modulus Operators

        /// <summary>
        ///     Multiply two BigIntegers returning the result.
        /// </summary>
        /// <remarks>
        ///     See Knuth.
        /// </remarks>
        /// <param name="leftSide">A BigInteger.</param>
        /// <param name="rightSide">A BigInteger</param>
        /// <returns></returns>
        public static BigInteger operator *(BigInteger leftSide,BigInteger rightSide)
        {
            if(ReferenceEquals(leftSide,null))
            {
                throw new ArgumentNullException("leftSide");
            }
            if(ReferenceEquals(rightSide,null))
            {
                throw new ArgumentNullException("rightSide");
            }

            var leftSideNeg=leftSide.IsNegative;
            var rightSideNeg=rightSide.IsNegative;

            leftSide=Abs(leftSide);
            rightSide=Abs(rightSide);

            var da=new DigitsArray(leftSide.m_digits.DataUsed+rightSide.m_digits.DataUsed);
            da.DataUsed=da.Count;

            for(var i=0;i<leftSide.m_digits.DataUsed;i++)
            {
                ulong carry=0;
                for(int j=0,k=i;j<rightSide.m_digits.DataUsed;j++, k++)
                {
                    var val=(leftSide.m_digits[i]*(ulong)rightSide.m_digits[j])+da[k]+carry;

                    da[k]=(uint)(val&DigitsArray.AllBits);
                    carry=(val>>DigitsArray.DataSizeBits);
                }

                if(carry!=0)
                {
                    da[i+rightSide.m_digits.DataUsed]=(uint)carry;
                }
            }

            //da.ResetDataUsed();
            var result=new BigInteger(da);
            return (leftSideNeg!=rightSideNeg ? -result : result);
        }

        /// <summary>
        ///     Multiply two BigIntegers returning the result.
        /// </summary>
        /// <param name="leftSide">A BigInteger.</param>
        /// <param name="rightSide">A BigInteger</param>
        /// <returns></returns>
        public static BigInteger Multiply(BigInteger leftSide,BigInteger rightSide)
        {
            return leftSide*rightSide;
        }

        /// <summary>
        ///     Divide a BigInteger by another BigInteger and returning the result.
        /// </summary>
        /// <param name="leftSide">A BigInteger divisor.</param>
        /// <param name="rightSide">A BigInteger dividend.</param>
        /// <returns>The BigInteger result.</returns>
        public static BigInteger operator /(BigInteger leftSide,BigInteger rightSide)
        {
            if(leftSide==null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if(rightSide==null)
            {
                throw new ArgumentNullException("rightSide");
            }

            if(rightSide.IsZero)
            {
                throw new DivideByZeroException();
            }

            var divisorNeg=rightSide.IsNegative;
            var dividendNeg=leftSide.IsNegative;

            leftSide=Abs(leftSide);
            rightSide=Abs(rightSide);

            if(leftSide<rightSide)
            {
                return new BigInteger(0);
            }

            BigInteger quotient;
            BigInteger remainder;
            Divide(leftSide,rightSide,out quotient,out remainder);

            return (dividendNeg!=divisorNeg ? -quotient : quotient);
        }

        /// <summary>
        ///     Divide a BigInteger by another BigInteger and returning the result.
        /// </summary>
        /// <param name="leftSide">A BigInteger divisor.</param>
        /// <param name="rightSide">A BigInteger dividend.</param>
        /// <returns>The BigInteger result.</returns>
        public static BigInteger Divide(BigInteger leftSide,BigInteger rightSide)
        {
            return leftSide/rightSide;
        }

        private static void Divide(BigInteger leftSide,BigInteger rightSide,out BigInteger quotient,out BigInteger remainder)
        {
            if(leftSide.IsZero)
            {
                quotient=new BigInteger();
                remainder=new BigInteger();
                return;
            }

            if(rightSide.m_digits.DataUsed==1)
            {
                SingleDivide(leftSide,rightSide,out quotient,out remainder);
            } else
            {
                MultiDivide(leftSide,rightSide,out quotient,out remainder);
            }
        }

        private static void MultiDivide(BigInteger leftSide,BigInteger rightSide,out BigInteger quotient,out BigInteger remainder)
        {
            if(rightSide.IsZero)
            {
                throw new DivideByZeroException();
            }

            var val=rightSide.m_digits[rightSide.m_digits.DataUsed-1];
            var d=0;
            for(var mask=DigitsArray.HiBitSet;mask!=0&&(val&mask)==0;mask>>=1)
            {
                d++;
            }

            var remainderLen=leftSide.m_digits.DataUsed+1;
            var remainderDat=new uint[remainderLen];
            leftSide.m_digits.CopyTo(remainderDat,0,leftSide.m_digits.DataUsed);

            DigitsArray.ShiftLeft(remainderDat,d);
            rightSide=rightSide<<d;

            ulong firstDivisor=rightSide.m_digits[rightSide.m_digits.DataUsed-1];
            ulong secondDivisor=(rightSide.m_digits.DataUsed<2 ? 0 : rightSide.m_digits[rightSide.m_digits.DataUsed-2]);

            var divisorLen=rightSide.m_digits.DataUsed+1;
            var dividendPart=new DigitsArray(divisorLen,divisorLen);
            var result=new uint[leftSide.m_digits.Count+1];
            var resultPos=0;

            var carryBit=(ulong)0x1<<DigitsArray.DataSizeBits; // 0x100000000
            for(int j=remainderLen-rightSide.m_digits.DataUsed,pos=remainderLen-1;j>0;j--, pos--)
            {
                var dividend=((ulong)remainderDat[pos]<<DigitsArray.DataSizeBits)+remainderDat[pos-1];
                var qHat=(dividend/firstDivisor);
                var rHat=(dividend%firstDivisor);

                while(pos>=2)
                {
                    if(qHat==carryBit||(qHat*secondDivisor)>((rHat<<DigitsArray.DataSizeBits)+remainderDat[pos-2]))
                    {
                        qHat--;
                        rHat+=firstDivisor;
                        if(rHat<carryBit)
                        {
                            continue;
                        }
                    }
                    break;
                }

                for(var h=0;h<divisorLen;h++)
                {
                    dividendPart[divisorLen-h-1]=remainderDat[pos-h];
                }

                var dTemp=new BigInteger(dividendPart);
                var rTemp=rightSide*(long)qHat;
                while(rTemp>dTemp)
                {
                    qHat--;
                    rTemp-=rightSide;
                }

                rTemp=dTemp-rTemp;
                for(var h=0;h<divisorLen;h++)
                {
                    remainderDat[pos-h]=rTemp.m_digits[rightSide.m_digits.DataUsed-h];
                }

                result[resultPos++]=(uint)qHat;
            }

            Array.Reverse(result,0,resultPos);
            quotient=new BigInteger(new DigitsArray(result));

            var n=DigitsArray.ShiftRight(remainderDat,d);
            var rDA=new DigitsArray(n,n);
            rDA.CopyFrom(remainderDat,0,0,rDA.DataUsed);
            remainder=new BigInteger(rDA);
        }

        private static void SingleDivide(BigInteger leftSide,BigInteger rightSide,out BigInteger quotient,out BigInteger remainder)
        {
            if(rightSide.IsZero)
            {
                throw new DivideByZeroException();
            }

            var remainderDigits=new DigitsArray(leftSide.m_digits);
            remainderDigits.ResetDataUsed();

            var pos=remainderDigits.DataUsed-1;
            ulong divisor=rightSide.m_digits[0];
            ulong dividend=remainderDigits[pos];

            var result=new uint[leftSide.m_digits.Count];
            leftSide.m_digits.CopyTo(result,0,result.Length);
            var resultPos=0;

            if(dividend>=divisor)
            {
                result[resultPos++]=(uint)(dividend/divisor);
                remainderDigits[pos]=(uint)(dividend%divisor);
            }
            pos--;

            while(pos>=0)
            {
                dividend=((ulong)(remainderDigits[pos+1])<<DigitsArray.DataSizeBits)+remainderDigits[pos];
                result[resultPos++]=(uint)(dividend/divisor);
                remainderDigits[pos+1]=0;
                remainderDigits[pos--]=(uint)(dividend%divisor);
            }
            remainder=new BigInteger(remainderDigits);

            var quotientDigits=new DigitsArray(resultPos+1,resultPos);
            var j=0;
            for(var i=quotientDigits.DataUsed-1;i>=0;i--, j++)
            {
                quotientDigits[j]=result[i];
            }
            quotient=new BigInteger(quotientDigits);
        }

        /// <summary>
        ///     Perform the modulus of a BigInteger with another BigInteger and return the result.
        /// </summary>
        /// <param name="leftSide">A BigInteger divisor.</param>
        /// <param name="rightSide">A BigInteger dividend.</param>
        /// <returns>The BigInteger result.</returns>
        public static BigInteger operator %(BigInteger leftSide,BigInteger rightSide)
        {
            if(leftSide==null)
            {
                throw new ArgumentNullException("leftSide");
            }

            if(rightSide==null)
            {
                throw new ArgumentNullException("rightSide");
            }

            if(rightSide.IsZero)
            {
                throw new DivideByZeroException();
            }

            BigInteger quotient;
            BigInteger remainder;

            var dividendNeg=leftSide.IsNegative;
            leftSide=Abs(leftSide);
            rightSide=Abs(rightSide);

            if(leftSide<rightSide)
            {
                return leftSide;
            }

            Divide(leftSide,rightSide,out quotient,out remainder);

            return (dividendNeg ? -remainder : remainder);
        }

        /// <summary>
        ///     Perform the modulus of a BigInteger with another BigInteger and return the result.
        /// </summary>
        /// <param name="leftSide">A BigInteger divisor.</param>
        /// <param name="rightSide">A BigInteger dividend.</param>
        /// <returns>The BigInteger result.</returns>
        public static BigInteger Modulus(BigInteger leftSide,BigInteger rightSide)
        {
            return leftSide%rightSide;
        }

        #endregion


        #region Bitwise Operator Overloads

        public static BigInteger operator &(BigInteger leftSide,BigInteger rightSide)
        {
            var len=Math.Max(leftSide.m_digits.DataUsed,rightSide.m_digits.DataUsed);
            var da=new DigitsArray(len,len);
            for(var idx=0;idx<len;idx++)
            {
                da[idx]=leftSide.m_digits[idx]&rightSide.m_digits[idx];
            }
            return new BigInteger(da);
        }

        public static BigInteger BitwiseAnd(BigInteger leftSide,BigInteger rightSide)
        {
            return leftSide&rightSide;
        }

        public static BigInteger operator |(BigInteger leftSide,BigInteger rightSide)
        {
            var len=Math.Max(leftSide.m_digits.DataUsed,rightSide.m_digits.DataUsed);
            var da=new DigitsArray(len,len);
            for(var idx=0;idx<len;idx++)
            {
                da[idx]=leftSide.m_digits[idx]|rightSide.m_digits[idx];
            }
            return new BigInteger(da);
        }

        public static BigInteger BitwiseOr(BigInteger leftSide,BigInteger rightSide)
        {
            return leftSide|rightSide;
        }

        public static BigInteger operator ^(BigInteger leftSide,BigInteger rightSide)
        {
            var len=Math.Max(leftSide.m_digits.DataUsed,rightSide.m_digits.DataUsed);
            var da=new DigitsArray(len,len);
            for(var idx=0;idx<len;idx++)
            {
                da[idx]=leftSide.m_digits[idx]^rightSide.m_digits[idx];
            }
            return new BigInteger(da);
        }

        public static BigInteger Xor(BigInteger leftSide,BigInteger rightSide)
        {
            return leftSide^rightSide;
        }

        public static BigInteger operator ~(BigInteger leftSide)
        {
            var da=new DigitsArray(leftSide.m_digits.Count);
            for(var idx=0;idx<da.Count;idx++)
            {
                da[idx]=~(leftSide.m_digits[idx]);
            }

            return new BigInteger(da);
        }

        public static BigInteger OnesComplement(BigInteger leftSide)
        {
            return ~leftSide;
        }

        #endregion


        #region Left and Right Shift Operator Overloads

        public static BigInteger operator <<(BigInteger leftSide,int shiftCount)
        {
            if(leftSide==null)
            {
                throw new ArgumentNullException("leftSide");
            }

            var da=new DigitsArray(leftSide.m_digits);
            da.DataUsed=da.ShiftLeftWithoutOverflow(shiftCount);

            return new BigInteger(da);
        }

        public static BigInteger LeftShift(BigInteger leftSide,int shiftCount)
        {
            return leftSide<<shiftCount;
        }

        public static BigInteger operator >>(BigInteger leftSide,int shiftCount)
        {
            if(leftSide==null)
            {
                throw new ArgumentNullException("leftSide");
            }

            var da=new DigitsArray(leftSide.m_digits);
            da.DataUsed=da.ShiftRight(shiftCount);

            if(leftSide.IsNegative)
            {
                for(var i=da.Count-1;i>=da.DataUsed;i--)
                {
                    da[i]=DigitsArray.AllBits;
                }

                var mask=DigitsArray.HiBitSet;
                for(var i=0;i<DigitsArray.DataSizeBits;i++)
                {
                    if((da[da.DataUsed-1]&mask)==DigitsArray.HiBitSet)
                    {
                        break;
                    }
                    da[da.DataUsed-1]|=mask;
                    mask>>=1;
                }
                da.DataUsed=da.Count;
            }

            return new BigInteger(da);
        }

        public static BigInteger RightShift(BigInteger leftSide,int shiftCount)
        {
            if(leftSide==null)
            {
                throw new ArgumentNullException("leftSide");
            }

            return leftSide>>shiftCount;
        }

        #endregion


        #region Relational Operator Overloads

        /// <summary>
        ///     Compare this instance to a specified object and returns indication of their relative value.
        /// </summary>
        /// <param name="value">An object to compare, or a null reference (<b>Nothing</b> in Visual Basic).</param>
        /// <returns>
        ///     A signed number indicating the relative value of this instance and <i>value</i>.
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Return Value</term>
        ///             <description>Description</description>
        ///         </listheader>
        ///         <item>
        ///             <term>Less than zero</term>
        ///             <description>This instance is less than <i>value</i>.</description>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <description>This instance is equal to <i>value</i>.</description>
        ///         </item>
        ///         <item>
        ///             <term>Greater than zero</term>
        ///             <description>
        ///                 This instance is greater than <i>value</i>.
        ///                 <para>-or-</para>
        ///                 <i>value</i> is a null reference (<b>Nothing</b> in Visual Basic).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </returns>
        public int CompareTo(BigInteger value)
        {
            return Compare(this,value);
        }

        /// <summary>
        ///     Compare two objects and return an indication of their relative value.
        /// </summary>
        /// <param name="leftSide">An object to compare, or a null reference (<b>Nothing</b> in Visual Basic).</param>
        /// <param name="rightSide">An object to compare, or a null reference (<b>Nothing</b> in Visual Basic).</param>
        /// <returns>
        ///     A signed number indicating the relative value of this instance and <i>value</i>.
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Return Value</term>
        ///             <description>Description</description>
        ///         </listheader>
        ///         <item>
        ///             <term>Less than zero</term>
        ///             <description>This instance is less than <i>value</i>.</description>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <description>This instance is equal to <i>value</i>.</description>
        ///         </item>
        ///         <item>
        ///             <term>Greater than zero</term>
        ///             <description>
        ///                 This instance is greater than <i>value</i>.
        ///                 <para>-or-</para>
        ///                 <i>value</i> is a null reference (<b>Nothing</b> in Visual Basic).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </returns>
        public static int Compare(BigInteger leftSide,BigInteger rightSide)
        {
            if(ReferenceEquals(leftSide,rightSide))
            {
                return 0;
            }

            if(ReferenceEquals(leftSide,null))
            {
                throw new ArgumentNullException("leftSide");
            }

            if(ReferenceEquals(rightSide,null))
            {
                throw new ArgumentNullException("rightSide");
            }

            if(leftSide>rightSide)
            {
                return 1;
            }
            if(leftSide==rightSide)
            {
                return 0;
            }
            return -1;
        }

        public static bool operator ==(BigInteger leftSide,BigInteger rightSide)
        {
            if(ReferenceEquals(leftSide,rightSide))
            {
                return true;
            }

            if(ReferenceEquals(leftSide,null)||ReferenceEquals(rightSide,null))
            {
                return false;
            }

            if(leftSide.IsNegative!=rightSide.IsNegative)
            {
                return false;
            }

            return leftSide.Equals(rightSide);
        }

        public static bool operator !=(BigInteger leftSide,BigInteger rightSide)
        {
            return !(leftSide==rightSide);
        }

        public static bool operator >(BigInteger leftSide,BigInteger rightSide)
        {
            if(ReferenceEquals(leftSide,null))
            {
                throw new ArgumentNullException("leftSide");
            }

            if(ReferenceEquals(rightSide,null))
            {
                throw new ArgumentNullException("rightSide");
            }

            if(leftSide.IsNegative!=rightSide.IsNegative)
            {
                return rightSide.IsNegative;
            }

            if(leftSide.m_digits.DataUsed!=rightSide.m_digits.DataUsed)
            {
                return leftSide.m_digits.DataUsed>rightSide.m_digits.DataUsed;
            }

            for(var idx=leftSide.m_digits.DataUsed-1;idx>=0;idx--)
            {
                if(leftSide.m_digits[idx]!=rightSide.m_digits[idx])
                {
                    return (leftSide.m_digits[idx]>rightSide.m_digits[idx]);
                }
            }
            return false;
        }

        public static bool operator <(BigInteger leftSide,BigInteger rightSide)
        {
            if(ReferenceEquals(leftSide,null))
            {
                throw new ArgumentNullException("leftSide");
            }

            if(ReferenceEquals(rightSide,null))
            {
                throw new ArgumentNullException("rightSide");
            }

            if(leftSide.IsNegative!=rightSide.IsNegative)
            {
                return leftSide.IsNegative;
            }

            if(leftSide.m_digits.DataUsed!=rightSide.m_digits.DataUsed)
            {
                return leftSide.m_digits.DataUsed<rightSide.m_digits.DataUsed;
            }

            for(var idx=leftSide.m_digits.DataUsed-1;idx>=0;idx--)
            {
                if(leftSide.m_digits[idx]!=rightSide.m_digits[idx])
                {
                    return (leftSide.m_digits[idx]<rightSide.m_digits[idx]);
                }
            }
            return false;
        }

        public static bool operator >=(BigInteger leftSide,BigInteger rightSide)
        {
            return Compare(leftSide,rightSide)>=0;
        }

        public static bool operator <=(BigInteger leftSide,BigInteger rightSide)
        {
            return Compare(leftSide,rightSide)<=0;
        }

        #endregion


        #region Object Overrides

        /// <summary>
        ///     Determines whether two Object instances are equal.
        /// </summary>
        /// <param name="obj">An <see cref="System.Object">Object</see> to compare with this instance.</param>
        /// <returns></returns>
        /// <seealso cref="System.Object">System.Object</seealso>
        public override bool Equals(object obj)
        {
            if(ReferenceEquals(obj,null))
            {
                return false;
            }

            if(ReferenceEquals(this,obj))
            {
                return true;
            }

            var c=(BigInteger)obj;
            if(m_digits.DataUsed!=c.m_digits.DataUsed)
            {
                return false;
            }

            for(var idx=0;idx<m_digits.DataUsed;idx++)
            {
                if(m_digits[idx]!=c.m_digits[idx])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer has code.</returns>
        /// <seealso cref="System.Object">System.Object</seealso>
        public override int GetHashCode()
        {
            return m_digits.GetHashCode();
        }

        /// <summary>
        ///     Converts the numeric value of this instance to its equivalent base 10 string representation.
        /// </summary>
        /// <returns>A <see cref="System.String">String</see> in base 10.</returns>
        /// <seealso cref="System.Object">System.Object</seealso>
        public override string ToString()
        {
            return ToString(10);
        }

        #endregion
    }
}