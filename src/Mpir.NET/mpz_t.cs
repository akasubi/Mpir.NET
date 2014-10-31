using System;
using System.Collections.Generic;
using System.Numerics;

// Disable warning about missing XML comments.
#pragma warning disable 1591

namespace Mpir.NET
{
    public class mpz_t : IDisposable, ICloneable, IConvertible, IComparable
    {
        #region Data
        private const uint s_defaultStringBase = 10u;

        public IntPtr val;
        private bool disposed = false;
        #endregion

        #region Creation and destruction

        /// Initializes a new mpz_t to 0.
        public mpz_t() { val = mpir.mpz_init(); }
        /// Initializes a new mpz_t to the same value as op.
        public mpz_t(mpz_t op)                { val = mpir.mpz_init_set(op);    }
        /// Initializes a new mpz_t to the unsigned int op.
        public mpz_t(uint op) { val = mpir.mpz_init_set_ui(op); }
        /// Initializes a new mpz_t to the int op.
        public mpz_t(int op) { val = mpir.mpz_init_set_si(op); }
        /// Initializes a new mpz_t to the double op.
        public mpz_t(double op) { val = mpir.mpz_init_set_d(op); }
        /// Initializes a new mpz_t to string s, parsed as an integer in the specified base.
        public mpz_t(string s, uint _base) { val = mpir.mpz_init_set_str(s, _base); }
        /// Initializes a new mpz_t to string s, parsed as an integer in base 10.
        public mpz_t(string s) : this(s, s_defaultStringBase) { }
        /// Initializes a new mpz_t to the BigInteger op.
        public mpz_t(BigInteger op) : this(op.ToByteArray(), -1) { }

        /// Initializes a new mpz_t to using MPIR mpz_init2. Only use if you need to
        /// avoid reallocations.
        // 
        // Initialization with mpz_init2 should not be confused with mpz_t construction
        // from a ulong. Thus, so we use a static construction function instead, and add
        // the dummy type init2_type to enable us to write a ctor with a unique signature.
        public static mpz_t init2(ulong n)  { return new mpz_t(init2_type.init2, n); }
        private enum init2_type { init2 }
        private mpz_t(init2_type dummy, ulong n) 
        { 
            val = mpir.mpz_init2(n);
        }

        /// Initializes a new mpz_t to the long op.
        public mpz_t(long op)
            : this()
        {
            var bytes = BitConverter.GetBytes(op);
            FromByteArray(bytes, BitConverter.IsLittleEndian ? -1 : 1);
        }

        /// Initializes a new mpz_t to the unsigned long op.
        public mpz_t(ulong op)
            : this()
        {
            var bytes = BitConverter.GetBytes(op);
            FromByteArray(bytes, BitConverter.IsLittleEndian ? -1 : 1);
        }

        /// Initializes a new mpz_t to the integer in the byte array bytes.
        /// Endianess is specified by order, which is 1 for big endian or -1 
        /// for little endian.
        public mpz_t(byte[] bytes, int order) : this()
        {
            FromByteArray(bytes, order);
        }

        ~mpz_t()
        {
            Dispose(false);
        }
        
        public void Dispose() 
        {
            Dispose(true);
            GC.SuppressFinalize(this);      
        }
        
        protected virtual void Dispose(bool disposing)
        {
            // There are no managed resources, so <disposing> is not used.
            if (!disposed) {
                // dispose unmanaged resources
                mpir.mpz_clear(this);
                disposed = true;   
            }
        }

        #endregion

        #region Import and export byte array

        /// Import the integer in the byte array bytes.
        /// Endianess is specified by order, which is 1 for big endian or -1 
        /// for little endian.
        public void FromByteArray(byte[] source, int order)
        {
            mpir.Mpir_mpz_import(this, (uint)source.Length, order, sizeof(byte), 0, 0u, source);
        }

        /// Import the integer in the byte array bytes, starting at startOffset
        /// and ending at endOffset.
        /// Endianess is specified by order, which is 1 for big endian or -1 
        /// for little endian.
        public void ImportByOffset(byte[] source, int startOffset, int endOffset, int order)
        {
            mpir.Mpir_mpz_import_by_offset(this, startOffset, endOffset, order, sizeof(byte), 0, 0u, source);
        }

        /// Export to the value to a byte array.
        /// Endianess is specified by order, which is 1 for big endian or -1 
        /// for little endian.
        public byte[] ToByteArray(int order)
        {
            return mpir.Mpir_mpz_export(order, (uint) sizeof(byte), 0, 0u, this);
        }
        #endregion

        // Almost everything below is copied from Emil Stefanov's BigInt 
        // http://www.emilstefanov.net/Projects/GnuMpDotNet/
        // with a few minor adjustments, e.g. datatype used 
        // and ++ and -- operators which now do in-place inrements/decrements).
        // All code handling Decimal is commented out, dute to some
        // unexpected behaviour.

        // TODO: Dispose temp mpz_t objects that are created by casts in operators.

        #region Predefined Values

        public static readonly mpz_t NegativeTen   = new mpz_t(-10);
        public static readonly mpz_t NegativeThree = new mpz_t(-3);
        public static readonly mpz_t NegativeTwo   = new mpz_t(-2);
        public static readonly mpz_t NegativeOne   = new mpz_t(-1);
        public static readonly mpz_t Zero          = new mpz_t(0);
        public static readonly mpz_t One           = new mpz_t(1);
        public static readonly mpz_t Two           = new mpz_t(2);
        public static readonly mpz_t Three         = new mpz_t(3);
        public static readonly mpz_t Ten           = new mpz_t(10);

        #endregion

        #region Operators

        public static mpz_t operator -(mpz_t x)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_neg(z, x);
            return z;
        }

        public static mpz_t operator ~(mpz_t x)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_com(z, x);
            return z;
        }

        public static mpz_t operator +(mpz_t x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_add(z, x, y);
            return z;
        }

        public static mpz_t operator +(mpz_t x, int y)
        {
            mpz_t z = new mpz_t();

            if(y >= 0)
            {
                mpir.mpz_add_ui(z, x, (uint)y);
            }
            else
            {
                mpir.mpz_sub_ui(z, x, (uint)(-y));
            }

            return z;
        }

        public static mpz_t operator +(int x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            if(x >= 0)
            {
                mpir.mpz_add_ui(z, y, (uint)x);
            }
            else
            {
                mpir.mpz_sub_ui(z, y, (uint)(-x));
            }

            return z;
        }

        public static mpz_t operator +(mpz_t x, uint y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_add_ui(z, x, y);
            return z;
        }

        public static mpz_t operator +(uint x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_add_ui(z, y, x);
            return z;
        }

        public static mpz_t operator -(mpz_t x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_sub(z, x, y);
            return z;
        }

        public static mpz_t operator -(int x, mpz_t y)
        {
            if(x >= 0)
            {
                mpz_t z = new mpz_t();
                mpir.mpz_ui_sub(z, (uint)x, y);
                return z;
            }
            else
            {
                mpz_t z = new mpz_t();
                mpir.mpz_add_ui(z, y, (uint)(-x));
                mpz_t z1 = -z;
                z.Dispose();
                return z1;
            }
        }

        public static mpz_t operator -(mpz_t x, int y)
        {
            mpz_t z = new mpz_t();

            if(y >= 0)
            {
                mpir.mpz_sub_ui(z, x, (uint)y);
            }
            else
            {
                mpir.mpz_add_ui(z, x, (uint)(-y));

            }

            return z;
        }

        public static mpz_t operator -(uint x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_ui_sub(z, x, y);
            return z;
        }

        public static mpz_t operator -(mpz_t x, uint y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_sub_ui(z, x, y);
            return z;
        }

        public static mpz_t operator ++(mpz_t x)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_add_ui(z, x, 1);
            mpir.mpz_set(x, z);
            z.Dispose();
            return x;
        }

        public static mpz_t operator --(mpz_t x)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_sub_ui(z, x, 1);
            mpir.mpz_set(x, z);
            z.Dispose();
            return x;
        }

        public static mpz_t operator *(mpz_t x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_mul(z, x, y);
            return z;
        }

        public static mpz_t operator *(int x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_mul_si(z, y, x);
            return z;
        }

        public static mpz_t operator *(mpz_t x, int y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_mul_si(z, x, y);
            return z;
        }

        public static mpz_t operator *(uint x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_mul_ui(z, y, x);
            return z;
        }

        public static mpz_t operator *(mpz_t x, uint y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_mul_ui(z, x, y);
            return z;
        }

        public static mpz_t operator /(mpz_t x, mpz_t y)
        {
            mpz_t quotient = new mpz_t();
            mpir.mpz_tdiv_q(quotient, x, y);
            return quotient;
        }

        public static mpz_t operator /(mpz_t x, int y)
        {
            if (y >= 0) {
                mpz_t quotient = new mpz_t();
                mpir.mpz_tdiv_q_ui(quotient, x, (uint)y);
                return quotient;
            } else {
                mpz_t quotient = new mpz_t();
                mpir.mpz_tdiv_q_ui(quotient, x, (uint)(-y));
                mpz_t negQ = -quotient;
                quotient.Dispose();
                return negQ;
            }

        }

        public static mpz_t operator /(mpz_t x, uint y)
        {
            mpz_t quotient = new mpz_t();
            mpir.mpz_tdiv_q_ui(quotient, x, y);
            return quotient;
        }

        public static mpz_t operator &(mpz_t x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_and(z, x, y);
            return z;
        }

        public static mpz_t operator |(mpz_t x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_ior(z, x, y);
            return z;
        }

        public static mpz_t operator ^(mpz_t x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_xor(z, x, y);
            return z;
        }

        public static mpz_t operator %(mpz_t x, mpz_t mod)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_mod(z, x, mod);
            return z;
        }

        public static mpz_t operator %(mpz_t x, int mod)
        {
            if(mod < 0)
                throw new ArgumentOutOfRangeException();

            mpz_t z = new mpz_t();
            mpir.mpz_fdiv_r_ui(z, x, (uint)mod);
            return z;
        }

        public static mpz_t operator %(mpz_t x, uint mod)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_fdiv_r_ui(z, x, mod);
            return z;
        }

        public static bool operator <(mpz_t x, mpz_t y)
        {
            return x.CompareTo(y) < 0;
        }

        public static bool operator <(int x, mpz_t y)
        {
            return y.CompareTo(x) > 0;
        }

        public static bool operator <(mpz_t x, int y)
        {
            return x.CompareTo(y) < 0;
        }

        public static bool operator <(uint x, mpz_t y)
        {
            return y.CompareTo(x) > 0;
        }

        public static bool operator <(mpz_t x, uint y)
        {
            return x.CompareTo(y) < 0;
        }

        public static bool operator <(long x, mpz_t y)
        {
            return (mpz_t)x < y;
        }

        public static bool operator <(mpz_t x, long y)
        {
            return x < (mpz_t)y;
        }

        public static bool operator <(ulong x, mpz_t y)
        {
            return (mpz_t)x < y;
        }

        public static bool operator <(mpz_t x, ulong y)
        {
            return x < (mpz_t)y;
        }

        public static bool operator <(float x, mpz_t y)
        {
            return y.CompareTo(x) > 0;
        }

        public static bool operator <(mpz_t x, float y)
        {
            return x.CompareTo(y) < 0;
        }

        public static bool operator <(double x, mpz_t y)
        {
            return y.CompareTo(x) > 0;
        }

        public static bool operator <(mpz_t x, double y)
        {
            return x.CompareTo(y) < 0;
        }

        //public static bool operator <(decimal x, mpz_t y)
        //{
        //    return y.CompareTo(x) > 0;
        //}

        //public static bool operator <(mpz_t x, decimal y)
        //{
        //    return x.CompareTo(y) < 0;
        //}

        public static bool operator <=(mpz_t x, mpz_t y)
        {
            return x.CompareTo(y) <= 0;
        }

        public static bool operator <=(int x, mpz_t y)
        {
            return y.CompareTo(x) >= 0;
        }

        public static bool operator <=(mpz_t x, int y)
        {
            return x.CompareTo(y) <= 0;
        }

        public static bool operator <=(uint x, mpz_t y)
        {
            return y.CompareTo(x) >= 0;
        }

        public static bool operator <=(mpz_t x, uint y)
        {
            return x.CompareTo(y) <= 0;
        }

        // TODO: Implement by accessing the data directly
        public static bool operator <=(long x, mpz_t y)
        {
            return (mpz_t)x <= y;
        }

        public static bool operator <=(mpz_t x, long y)
        {
            return x <= (mpz_t)y;
        }

        public static bool operator <=(ulong x, mpz_t y)
        {
            return (mpz_t)x <= y;
        }

        public static bool operator <=(mpz_t x, ulong y)
        {
            return x <= (mpz_t)y;
        }

        public static bool operator <=(float x, mpz_t y)
        {
            return y.CompareTo(x) >= 0;
        }

        public static bool operator <=(mpz_t x, float y)
        {
            return x.CompareTo(y) <= 0;
        }

        public static bool operator <=(double x, mpz_t y)
        {
            return y.CompareTo(x) >= 0;
        }

        public static bool operator <=(mpz_t x, double y)
        {
            return x.CompareTo(y) <= 0;
        }

        //public static bool operator <=(decimal x, mpz_t y)
        //{
        //    return y.CompareTo(x) >= 0;
        //}

        //public static bool operator <=(mpz_t x, decimal y)
        //{
        //    return x.CompareTo(y) <= 0;
        //}

        public static bool operator >(mpz_t x, mpz_t y)
        {
            return x.CompareTo(y) > 0;
        }

        public static bool operator >(int x, mpz_t y)
        {
            return y.CompareTo(x) < 0;
        }

        public static bool operator >(mpz_t x, int y)
        {
            return x.CompareTo(y) > 0;
        }

        public static bool operator >(uint x, mpz_t y)
        {
            return y.CompareTo(x) < 0;
        }

        public static bool operator >(mpz_t x, uint y)
        {
            return x.CompareTo(y) > 0;
        }

        // TODO: Implement by accessing the data directly
        public static bool operator >(long x, mpz_t y)
        {
            return (mpz_t)x > y;
        }

        // TODO: Implement by accessing the data directly
        public static bool operator >(mpz_t x, long y)
        {
            return x > (mpz_t)y;
        }

        // TODO: Implement by accessing the data directly
        public static bool operator >(ulong x, mpz_t y)
        {
            return (mpz_t)x > y;
        }

        // TODO: Implement by accessing the data directly
        public static bool operator >(mpz_t x, ulong y)
        {
            return x > (mpz_t)y;
        }

        public static bool operator >(float x, mpz_t y)
        {
            return y.CompareTo(x) < 0;
        }

        public static bool operator >(mpz_t x, float y)
        {
            return x.CompareTo(y) > 0;
        }

        public static bool operator >(double x, mpz_t y)
        {
            return y.CompareTo(x) < 0;
        }

        public static bool operator >(mpz_t x, double y)
        {
            return x.CompareTo(y) > 0;
        }

        //public static bool operator >(decimal x, mpz_t y)
        //{
        //    return y.CompareTo(x) < 0;
        //}

        //public static bool operator >(mpz_t x, decimal y)
        //{
        //    return x.CompareTo(y) > 0;
        //}

        public static bool operator >=(mpz_t x, mpz_t y)
        {
            return x.CompareTo(y) >= 0;
        }

        public static bool operator >=(int x, mpz_t y)
        {
            return y.CompareTo(x) <= 0;
        }

        public static bool operator >=(mpz_t x, int y)
        {
            return x.CompareTo(y) >= 0;
        }

        public static bool operator >=(uint x, mpz_t y)
        {
            return y.CompareTo(x) <= 0;
        }

        public static bool operator >=(mpz_t x, uint y)
        {
            return x.CompareTo(y) >= 0;
        }

        // TODO: Implement by accessing the data directly
        public static bool operator >=(long x, mpz_t y)
        {
            return (mpz_t)x >= y;
        }

        // TODO: Implement by accessing the data directly
        public static bool operator >=(mpz_t x, long y)
        {
            return x >= (mpz_t)y;
        }

        // TODO: Implement by accessing the data directly
        public static bool operator >=(ulong x, mpz_t y)
        {
            return (mpz_t)x >= y;
        }

        // TODO: Implement by accessing the data directly
        public static bool operator >=(mpz_t x, ulong y)
        {
            return x >= (mpz_t)y;
        }

        public static bool operator >=(float x, mpz_t y)
        {
            return y.CompareTo(x) <= 0;
        }

        public static bool operator >=(mpz_t x, float y)
        {
            return x.CompareTo(y) >= 0;
        }

        public static bool operator >=(double x, mpz_t y)
        {
            return y.CompareTo(x) <= 0;
        }

        public static bool operator >=(mpz_t x, double y)
        {
            return x.CompareTo(y) >= 0;
        }

        //public static bool operator >=(decimal x, mpz_t y)
        //{
        //    return y.CompareTo(x) <= 0;
        //}

        //public static bool operator >=(mpz_t x, decimal y)
        //{
        //    return x.CompareTo(y) >= 0;
        //}

        public static mpz_t operator <<(mpz_t x, int shiftAmount)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_mul_2exp(z, x, (uint)shiftAmount);
            return z;
        }

        public static mpz_t operator >>(mpz_t x, int shiftAmount)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_tdiv_q_2exp(z, x, (uint)shiftAmount);
            return z;
        }

        public int this[int bitIndex]
        {
            get
            {
                return mpir.mpz_tstbit(this, (uint)bitIndex);
            }
        }

        public mpz_t ChangeBit(int bitIndex, int value)
        {
            mpz_t z = new mpz_t(this);

            if(value == 0)
                mpir.mpz_clrbit(z, (uint)bitIndex);
            else
                mpir.mpz_setbit(z, (uint)bitIndex);

            return z;
        }

        #endregion

        #region Basic Arithmatic

        public mpz_t Negate()
        {
            return -this;
        }

        public mpz_t Complement()
        {
            return (~this);
        }

        public mpz_t Add(mpz_t x)
        {
            return this + x;
        }

        public mpz_t Add(int x)
        {
            return this + x;
        }

        public mpz_t Add(uint x)
        {
            return this + x;
        }

        public mpz_t Subtract(mpz_t x)
        {
            return this - x;
        }

        public mpz_t Subtract(int x)
        {
            return this - x;
        }

        public mpz_t Subtract(uint x)
        {
            return this - x;
        }

        public mpz_t Multiply(mpz_t x)
        {
            return this * x;
        }

        public mpz_t Multiply(int x)
        {
            return this * x;
        }

        public mpz_t Multiply(uint x)
        {
            return this * x;
        }

        public mpz_t Square()
        {
            return this * this;
        }

        public mpz_t Divide(mpz_t x)
        {
            return this / x;
        }

        public mpz_t Divide(int x)
        {
            return this / x;
        }

        public mpz_t Divide(uint x)
        {
            return this / x;
        }

        public mpz_t Divide(mpz_t x, out mpz_t remainder)
        {
            mpz_t quotient = new mpz_t();
            remainder = new mpz_t();
            mpir.mpz_tdiv_qr(quotient, remainder, this, x);
            return quotient;
        }

        public mpz_t Divide(int x, out mpz_t remainder)
        {
            mpz_t quotient = new mpz_t();
            remainder = new mpz_t();

            if(x >= 0)
            {
                mpir.mpz_tdiv_qr_ui(quotient, remainder, this, (uint)x);
                return quotient;
            }
            else
            {
                mpir.mpz_tdiv_qr_ui(quotient, remainder, this, (uint)(-x));
                mpz_t res = -quotient;
                quotient.Dispose();
                return res;
            }
        }

        public mpz_t Divide(int x, out int remainder)
        {
            mpz_t quotient = new mpz_t();

            if(x >= 0)
            {
                remainder = (int)mpir.mpz_tdiv_q_ui(quotient, this, (uint)x);
                return quotient;
            }
            else
            {
                remainder = -(int)mpir.mpz_tdiv_q_ui(quotient, this, (uint)(-x));
                mpz_t res = -quotient;
                quotient.Dispose();
                return res;
            }
        }

        public mpz_t Divide(uint x, out mpz_t remainder)
        {
            mpz_t quotient = new mpz_t();
            remainder = new mpz_t();
            mpir.mpz_tdiv_qr_ui(quotient, remainder, this, x);
            return quotient;
        }

        public mpz_t Divide(uint x, out uint remainder)
        {
            // Unsure about the below exception for negative numbers. It's in Stefanov's 
            // original code, but that limitation isn't mentioned in 
            // http://gmplib.org/manual/Integer-Division.html#Integer-Division.
            //if(this.ChunkCount < 0)
            //    throw new InvalidOperationException("This method may not be called when the instance represents a negative number.");

            mpz_t quotient = new mpz_t();
            remainder = mpir.mpz_tdiv_q_ui(quotient, this, x);
            return quotient;
        }

        public mpz_t Divide(uint x, out int remainder)
        {
            mpz_t quotient = new mpz_t();
            uint uintRemainder = mpir.mpz_tdiv_q_ui(quotient, this, x);
            if(uintRemainder > (uint)int.MaxValue)
                throw new OverflowException();

            if(this >= 0)
                remainder = (int)uintRemainder;
            else
                remainder = -(int)uintRemainder;

            return quotient;
        }

        public mpz_t Remainder(mpz_t x)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_tdiv_r(z, this, x);
            return z;
        }

        public bool IsDivisibleBy(mpz_t x)
        {
            return mpir.mpz_divisible_p(this, x) != 0;
        }

        public bool IsDivisibleBy(int x)
        {
            if(x >= 0)
                return mpir.mpz_divisible_ui_p(this, (uint)x) != 0;
            else
                return mpir.mpz_divisible_ui_p(this, (uint)(-x)) != 0;
        }

        public bool IsDivisibleBy(uint x)
        {
            return mpir.mpz_divisible_ui_p(this, x) != 0;
        }

        /// <summary>
        /// Divides exactly. Only works when the division is gauranteed to be exact (there is no remainder).
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public mpz_t DivideExactly(mpz_t x)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_divexact(z, this, x);
            return z;
        }

        public mpz_t DivideExactly(int x)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_divexact_ui(z, this, (uint)x);

            if (x < 0) {
                mpz_t res = -z;
                z.Dispose();
                return res;
            } else {
                return z;
            }

        }

        public mpz_t DivideExactly(uint x)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_divexact_ui(z, this, x);
            return z;
        }

        public mpz_t DivideMod(mpz_t x, mpz_t mod)
        {
            return (this * x.InvertMod(mod)) % mod;
        }

        public mpz_t And(mpz_t x)
        {
            return (this & x);
        }

        public mpz_t Or(mpz_t x)
        {
            return (this | x);
        }

        public mpz_t Xor(mpz_t x)
        {
            return (this ^ x);
        }

        public mpz_t Mod(mpz_t mod)
        {
            return (this % mod);
        }

        public mpz_t Mod(int mod)
        {
            return (this % mod);
        }

        public mpz_t Mod(uint mod)
        {
            return (this % mod);
        }

        public int ModAsInt32(int mod)
        {
            if(mod < 0)
                throw new ArgumentOutOfRangeException();

            return (int)mpir.mpz_fdiv_ui(this, (uint)mod);
        }

        public uint ModAsUInt32(uint mod)
        {
            return mpir.mpz_fdiv_ui(this, mod);
        }

        public mpz_t ShiftLeft(int shiftAmount)
        {
            return (this << shiftAmount);
        }

        public mpz_t ShiftRight(int shiftAmount)
        {
            return (this >> shiftAmount);
        }

        public mpz_t PowerMod(mpz_t exponent, mpz_t mod)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_powm(z, this, exponent, mod);
            return z;
        }

        public mpz_t PowerMod(int exponent, mpz_t mod)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_powm_ui(z, this, (uint)exponent, mod);
            return z;
        }

        public mpz_t PowerMod(uint exponent, mpz_t mod)
        {
            mpz_t z = new mpz_t();
            if(exponent >= 0)
            {
                mpir.mpz_powm_ui(z, this, exponent, mod);
            }
            else
            {
                mpz_t bigExponent = exponent;
                mpz_t inverse = bigExponent.InvertMod(mod);
                mpir.mpz_powm_ui(z, inverse, exponent, mod);
            }

            return z;
        }

        public mpz_t Power(int exponent)
        {
            if(exponent < 0)
                throw new ArgumentOutOfRangeException();

            mpz_t z = new mpz_t();
            mpir.mpz_pow_ui(z, this, (uint)exponent);
            return z;
        }

        public mpz_t Power(uint exponent)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_pow_ui(z, this, exponent);
            return z;
        }

        public static mpz_t Power(int x, int exponent)
        {
            if(exponent < 0)
                throw new ArgumentOutOfRangeException();

            mpz_t z = new mpz_t();
            mpir.mpz_ui_pow_ui(z, (uint)x, (uint)exponent);
            return z;
        }

        public static mpz_t Power(uint x, uint exponent)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_ui_pow_ui(z, x, exponent);
            return z;
        }

        public mpz_t InvertMod(mpz_t mod)
        {
            mpz_t z = new mpz_t();
            int status = mpir.mpz_invert(z, this, mod);
            if(status == 0)
                throw new ArithmeticException("This modular inverse does not exists.");
            return z;
        }

        public bool TryInvertMod(mpz_t mod, out mpz_t result)
        {
            mpz_t z = new mpz_t();
            int status = mpir.mpz_invert(z, this, mod);

            if(status == 0)
            {
                result = null;
                return false;
            }
            else
            {
                result = z;
                return true;
            }
        }

        public bool InverseModExists(mpz_t mod)
        {
            mpz_t result;
            TryInvertMod(mod, out result);
            return true;
        }

        public int BitLength
        {
            get
            {
                return (int)mpir.mpz_sizeinbase(this, 2);
            }
        }

        #endregion

        #region Roots

        public mpz_t Sqrt()
        {
            mpz_t z = new mpz_t();
            mpir.mpz_sqrt(z, this);
            return z;
        }

        public mpz_t Sqrt(out mpz_t remainder)
        {
            mpz_t z = new mpz_t();
            remainder = new mpz_t();
            mpir.mpz_sqrtrem(z, remainder, this);
            return z;
        }

        public mpz_t Sqrt(out bool isExact)
        {
            mpz_t z = new mpz_t();
            int result = mpir.mpz_root(z, this, 2);
            isExact = result != 0;
            return z;
        }

        public mpz_t Root(int n)
        {
            if(n < 0)
                throw new ArgumentOutOfRangeException();

            mpz_t z = new mpz_t();
            mpir.mpz_root(z, this, (uint)n);
            return z;
        }

        public mpz_t Root(uint n)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_root(z, this, n);
            return z;
        }

        public mpz_t Root(int n, out bool isExact)
        {
            if(n < 0)
                throw new ArgumentOutOfRangeException();

            mpz_t z = new mpz_t();
            int result = mpir.mpz_root(z, this, (uint)n);
            isExact = result != 0;
            return z;
        }

        public mpz_t Root(uint n, out bool isExact)
        {
            mpz_t z = new mpz_t();
            int result = mpir.mpz_root(z, this, n);
            isExact = result != 0;
            return z;
        }

        public mpz_t Root(int n, out mpz_t remainder)
        {
            if(n < 0)
                throw new ArgumentOutOfRangeException();

            mpz_t z = new mpz_t();
            remainder = new mpz_t();
            mpir.mpz_rootrem(z, remainder, this, (uint)n);
            return z;
        }

        public mpz_t Root(uint n, out mpz_t remainder)
        {
            mpz_t z = new mpz_t();
            remainder = new mpz_t();
            mpir.mpz_rootrem(z, remainder, this, n);
            return z;
        }

        public bool IsPerfectSquare()
        {
            return mpir.mpz_perfect_square_p(this) != 0;
        }

        public bool IsPerfectPower()
        {
            // There is a known issue with this function for negative inputs in GMP 4.2.4.
            // Haven't heard of any issues in MPIR 5.x though.
            return mpir.mpz_perfect_power_p(this) != 0;
        }

        #endregion

        #region Number Theoretic Functions

        public bool IsProbablyPrimeRabinMiller(uint repetitions)
        {
            int result = mpir.mpz_probab_prime_p(this, repetitions);

            return result != 0;
        }

        // TODO: Create a version of this method which takes in a parameter to represent how well tested the prime should be.
        public mpz_t NextPrimeGMP()
        {
            mpz_t z = new mpz_t();
            mpir.mpz_nextprime(z, this);
            return z;
        }

        public static mpz_t Gcd(mpz_t x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_gcd(z, x, y);
            return z;
        }

        public static mpz_t Gcd(mpz_t x, int y)
        {
            mpz_t z = new mpz_t();

            if(y >= 0)
                mpir.mpz_gcd_ui(z, x, (uint)y);
            else
                mpir.mpz_gcd_ui(z, x, (uint)(-y));

            return z;
        }

        public static mpz_t Gcd(int x, mpz_t y)
        {
            mpz_t z = new mpz_t();

            if(x >= 0)
                mpir.mpz_gcd_ui(z, y, (uint)x);
            else
                mpir.mpz_gcd_ui(z, y, (uint)(-x));

            return z;
        }

        public static mpz_t Gcd(mpz_t x, uint y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_gcd_ui(z, x, y);
            return z;
        }

        public static mpz_t Gcd(uint x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_gcd_ui(z, y, x);
            return z;
        }

        public static mpz_t Gcd(mpz_t x, mpz_t y, out mpz_t a, out mpz_t b)
        {
            mpz_t z = new mpz_t();
            a = new mpz_t();
            b = new mpz_t();
            mpir.mpz_gcdext(z, a, b, x, y);
            return z;
        }

        public static mpz_t Gcd(mpz_t x, mpz_t y, out mpz_t a)
        {
            mpz_t z = new mpz_t();
            a = new mpz_t();
            mpir.mpz_gcdext(z, a, null, x, y);
            return z;
        }

        public static mpz_t Lcm(mpz_t x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_lcm(z, x, y);
            return z;
        }

        public static mpz_t Lcm(mpz_t x, int y)
        {
            mpz_t z = new mpz_t();

            if(y >= 0)
                mpir.mpz_lcm_ui(z, x, (uint)y);
            else
                mpir.mpz_lcm_ui(z, x, (uint)(-y));

            return z;
        }

        public static mpz_t Lcm(int x, mpz_t y)
        {
            mpz_t z = new mpz_t();

            if(x >= 0)
                mpir.mpz_lcm_ui(z, y, (uint)x);
            else
                mpir.mpz_lcm_ui(z, y, (uint)(-x));

            return z;
        }

        public static mpz_t Lcm(mpz_t x, uint y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_lcm_ui(z, x, y);
            return z;
        }

        public static mpz_t Lcm(uint x, mpz_t y)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_lcm_ui(z, y, x);
            return z;
        }

        public static int LegendreSymbol(mpz_t x, mpz_t primeY)
        {
            System.Diagnostics.Debug.Assert(primeY != 2); // Not defined for 2

            return mpir.mpz_jacobi(x, primeY);
        }

        public static int JacobiSymbol(mpz_t x, mpz_t y)
        {
            //IsEven not yet implemented, so commented out.
            //if(y.IsEven || y < 0)
            //    throw new ArgumentException();

            return mpir.mpz_jacobi(x, y);
        }

        public static int JacobiSymbol(mpz_t x, int y)
        {
            if((y & 1) == 0 || y < 0)
                throw new ArgumentException();

            return mpir.mpz_kronecker_si(x, y);
        }

        public static int JacobiSymbol(int x, mpz_t y)
        {
            //IsEven not yet implemented, so commented out.
            //if(y.IsEven || y < 0)
            //	throw new ArgumentException();

            return mpir.mpz_si_kronecker(x, y);
        }

        public static int JacobiSymbol(mpz_t x, uint y)
        {
            if((y & 1) == 0)
                throw new ArgumentException();

            return mpir.mpz_kronecker_ui(x, y);
        }

        public static int JacobiSymbol(uint x, mpz_t y)
        {
            //IsEven not yet implemented, so commented out.
            //if(y.IsEven)
            //    throw new ArgumentException();

            return mpir.mpz_ui_kronecker(x, y);
        }

        public static int KroneckerSymbol(mpz_t x, mpz_t y)
        {
            return mpir.mpz_kronecker(x, y);
        }

        public static int KroneckerSymbol(mpz_t x, int y)
        {
            return mpir.mpz_kronecker_si(x, y);
        }

        public static int KroneckerSymbol(int x, mpz_t y)
        {
            return mpir.mpz_si_kronecker(x, y);
        }

        public static int KroneckerSymbol(mpz_t x, uint y)
        {
            return mpir.mpz_kronecker_ui(x, y);
        }

        public static int KroneckerSymbol(uint x, mpz_t y)
        {
            return mpir.mpz_ui_kronecker(x, y);
        }

        public mpz_t RemoveFactor(mpz_t factor)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_remove(z, this, factor);
            return z;
        }

        public mpz_t RemoveFactor(mpz_t factor, out int count)
        {
            mpz_t z = new mpz_t();
            count = (int)mpir.mpz_remove(z, this, factor);
            return z;
        }

        public static mpz_t Factorial(int x)
        {
            if(x < 0)
                throw new ArgumentOutOfRangeException();

            mpz_t z = new mpz_t();
            mpir.mpz_fac_ui(z, (uint)x);
            return z;
        }

        public static mpz_t Factorial(uint x)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_fac_ui(z, x);
            return z;
        }

        public static mpz_t Binomial(mpz_t n, uint k)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_bin_ui(z, n, k);
            return z;
        }

        public static mpz_t Binomial(mpz_t n, int k)
        {
            if(k < 0)
                throw new ArgumentOutOfRangeException();

            mpz_t z = new mpz_t();
            mpir.mpz_bin_ui(z, n, (uint)k);
            return z;
        }

        public static mpz_t Binomial(uint n, uint k)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_bin_uiui(z, n, k);
            return z;
        }

        public static mpz_t Binomial(int n, int k)
        {
            if(k < 0)
                throw new ArgumentOutOfRangeException();

            mpz_t z = new mpz_t();

            if(n >= 0)
            {
                mpir.mpz_bin_uiui(z, (uint)n, (uint)k);
                return z;
            }
            else
            {
                // Use the identity bin(n,k) = (-1)^k * bin(-n+k-1,k)
                mpir.mpz_bin_uiui(z, (uint)(-n + k - 1), (uint)k);

                if ((k & 1) != 0) {
                    mpz_t res = -z;
                    z.Dispose();
                    return res;
                } else {
                    return z;
                }
            }

        }

        public static mpz_t Fibonacci(int n)
        {
            if(n < 0)
                throw new ArgumentOutOfRangeException();

            mpz_t z = new mpz_t();
            mpir.mpz_fib_ui(z, (uint)n);
            return z;
        }

        public static mpz_t Fibonacci(uint n)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_fib_ui(z, n);
            return z;
        }

        public static mpz_t Fibonacci(int n, out mpz_t previous)
        {
            if(n < 0)
                throw new ArgumentOutOfRangeException();

            mpz_t z = new mpz_t();
            previous = new mpz_t();
            mpir.mpz_fib2_ui(z, previous, (uint)n);
            return z;
        }

        public static mpz_t Fibonacci(uint n, out mpz_t previous)
        {
            mpz_t z = new mpz_t();
            previous = new mpz_t();
            mpir.mpz_fib2_ui(z, previous, n);
            return z;
        }

        public static mpz_t Lucas(int n)
        {
            if(n < 0)
                throw new ArgumentOutOfRangeException();

            mpz_t z = new mpz_t();
            mpir.mpz_lucnum_ui(z, (uint)n);
            return z;
        }

        public static mpz_t Lucas(uint n)
        {
            mpz_t z = new mpz_t();
            mpir.mpz_lucnum_ui(z, n);
            return z;
        }

        public static mpz_t Lucas(int n, out mpz_t previous)
        {
            if(n < 0)
                throw new ArgumentOutOfRangeException();

            mpz_t z = new mpz_t();
            previous = new mpz_t();
            mpir.mpz_lucnum2_ui(z, previous, (uint)n);
            return z;
        }

        public static mpz_t Lucas(uint n, out mpz_t previous)
        {
            mpz_t z = new mpz_t();
            previous = new mpz_t();
            mpir.mpz_lucnum2_ui(z, previous, n);
            return z;
        }

        #endregion

        #region Bitwise Functions

        public int CountOnes()
        {
            return (int)mpir.mpz_popcount(this);
        }

        public int HammingDistance(mpz_t x, mpz_t y)
        {
            return (int)mpir.mpz_hamdist(x, y);
        }

        public int IndexOfZero(int startingIndex)
        {
            unchecked
            {
                if(startingIndex < 0)
                    throw new ArgumentOutOfRangeException();

                // Note that the result might be uint.MaxValue in which case it gets cast to -1, which is what is intended.
                return (int)mpir.mpz_scan0(this, (uint)startingIndex);
            }
        }

        public int IndexOfOne(int startingIndex)
        {
            unchecked
            {
                if(startingIndex < 0)
                    throw new ArgumentOutOfRangeException();

                // Note that the result might be uint.MaxValue in which case it gets cast to -1, which is what is intended.
                return (int)mpir.mpz_scan1(this, (uint)startingIndex);
            }
        }

        #endregion

        #region Comparing

        public override int GetHashCode()
        {
            uint hash = 0;

            var bytes = ToByteArray(-1);

            int len = bytes.Length;  // Make sure it's only evaluated once.
            int shift = 0;
            for (int i = 0; i < len; i++) {
                hash ^= (uint) bytes[i] << shift;
                shift = (shift+8) & 0x1F;
            }

            return (int)hash;
        }

        public bool Equals(mpz_t other)
        {
            if(object.ReferenceEquals(other, null))
                return false;

            return Compare(this, other) == 0;
        }

        public override bool Equals(object obj)
        {
            if(object.ReferenceEquals(obj, null))
                return false;

            mpz_t objAsBigInt = obj as mpz_t;

            if(object.ReferenceEquals(objAsBigInt, null))
            {
                if(obj is int)
                    return this == (int)obj;
                else if(obj is uint)
                    return this == (uint)obj;
                else if(obj is long)
                    return this == (long)obj;
                else if(obj is ulong)
                    return this == (ulong)obj;
                else if(obj is double)
                    return this == (double)obj;
                else if(obj is float)
                    return this == (float)obj;
                else if(obj is short)
                    return this == (short)obj;
                else if(obj is ushort)
                    return this == (ushort)obj;
                else if(obj is byte)
                    return this == (byte)obj;
                else if(obj is sbyte)
                    return this == (sbyte)obj;
                //else if(obj is decimal)
                //    return this == (decimal)obj;

                return false;
            }

            return this.CompareTo(objAsBigInt) == 0;
        }

        public bool Equals(int other)
        {
            return this.CompareTo(other) == 0;
        }

        public bool Equals(uint other)
        {
            return this.CompareTo(other) == 0;
        }

        public bool Equals(long other)
        {
            return this.CompareTo(other) == 0;
        }

        public bool Equals(ulong other)
        {
            return this.CompareTo(other) == 0;
        }

        public bool Equals(double other)
        {
            return this.CompareTo(other) == 0;
        }

        //public bool Equals(decimal other)
        //{
        //    return this.CompareTo(other) == 0;
        //}

        public bool EqualsMod(mpz_t x, mpz_t mod)
        {
            return mpir.mpz_congruent_p(this, x, mod) != 0;
        }

        public bool EqualsMod(int x, int mod)
        {
            if(mod < 0)
                throw new ArgumentOutOfRangeException();

            if(x >= 0)
            {
                return mpir.mpz_congruent_ui_p(this, (uint)x, (uint)mod) != 0;
            }
            else
            {
                uint xAsUint = (uint)((x % mod) + mod);
                return mpir.mpz_congruent_ui_p(this, xAsUint, (uint)mod) != 0;
            }
        }

        public bool EqualsMod(uint x, uint mod)
        {
            return mpir.mpz_congruent_ui_p(this, x, mod) != 0;
        }

        public static bool operator ==(mpz_t x, mpz_t y)
        {
            bool xNull = object.ReferenceEquals(x, null);
            bool yNull = object.ReferenceEquals(y, null);

            if(xNull || yNull)
                return xNull && yNull;

            return x.CompareTo(y) == 0;
        }

        public static bool operator ==(int x, mpz_t y)
        {
            if(object.ReferenceEquals(y, null))
                return false;

            return y.CompareTo(x) == 0;
        }

        public static bool operator ==(mpz_t x, int y)
        {
            if(object.ReferenceEquals(x, null))
                return false;

            return x.CompareTo(y) == 0;
        }

        public static bool operator ==(uint x, mpz_t y)
        {
            if(object.ReferenceEquals(y, null))
                return false;

            return y.CompareTo(x) == 0;
        }

        public static bool operator ==(mpz_t x, uint y)
        {
            if(object.ReferenceEquals(x, null))
                return false;

            return x.CompareTo(y) == 0;
        }

        // TODO: Optimize this by accessing memory directly.
        public static bool operator ==(long x, mpz_t y)
        {
            if(object.ReferenceEquals(y, null))
                return false;

            return y.CompareTo(x) == 0;
        }

        // TODO: Optimize this by accessing memory directly.
        public static bool operator ==(mpz_t x, long y)
        {
            if(object.ReferenceEquals(x, null))
                return false;

            return x.CompareTo(y) == 0;
        }

        // TODO: Optimize this by accessing memory directly.
        public static bool operator ==(ulong x, mpz_t y)
        {
            if(object.ReferenceEquals(y, null))
                return false;

            return y.CompareTo(x) == 0;
        }

        // TODO: Optimize this by accessing memory directly.
        public static bool operator ==(mpz_t x, ulong y)
        {
            if(object.ReferenceEquals(x, null))
                return false;

            return x.CompareTo(y) == 0;
        }

        public static bool operator ==(float x, mpz_t y)
        {
            if(object.ReferenceEquals(y, null))
                return false;

            return y.CompareTo(x) == 0;
        }

        public static bool operator ==(mpz_t x, float y)
        {
            if(object.ReferenceEquals(x, null))
                return false;

            return x.CompareTo(y) == 0;
        }

        public static bool operator ==(double x, mpz_t y)
        {
            if(object.ReferenceEquals(y, null))
                return false;

            return y.CompareTo(x) == 0;
        }

        public static bool operator ==(mpz_t x, double y)
        {
            if(object.ReferenceEquals(x, null))
                return false;

            return x.CompareTo(y) == 0;
        }

        //public static bool operator ==(decimal x, mpz_t y)
        //{
        //    if(object.ReferenceEquals(y, null))
        //        return false;

        //    return y.CompareTo(x) == 0;
        //}

        //public static bool operator ==(mpz_t x, decimal y)
        //{
        //    if(object.ReferenceEquals(x, null))
        //        return false;

        //    return x.CompareTo(y) == 0;
        //}

        public static bool operator !=(mpz_t x, mpz_t y)
        {
            bool xNull = object.ReferenceEquals(x, null);
            bool yNull = object.ReferenceEquals(y, null);

            if(xNull || yNull)
                return !(xNull && yNull);

            return x.CompareTo(y) != 0;
        }

        public static bool operator !=(int x, mpz_t y)
        {
            if(object.ReferenceEquals(y, null))
                return true;

            return y.CompareTo(x) != 0;
        }

        public static bool operator !=(mpz_t x, int y)
        {
            if(object.ReferenceEquals(x, null))
                return true;

            return x.CompareTo(y) != 0;
        }

        public static bool operator !=(uint x, mpz_t y)
        {
            if(object.ReferenceEquals(y, null))
                return true;

            return y.CompareTo(x) != 0;
        }

        public static bool operator !=(mpz_t x, uint y)
        {
            if(object.ReferenceEquals(x, null))
                return true;

            return x.CompareTo(y) != 0;
        }

        // TODO: Optimize this by accessing memory directly
        public static bool operator !=(long x, mpz_t y)
        {
            if(object.ReferenceEquals(y, null))
                return true;

            return y.CompareTo((mpz_t)x) != 0;
        }

        // TODO: Optimize this by accessing memory directly
        public static bool operator !=(mpz_t x, long y)
        {
            if(object.ReferenceEquals(x, null))
                return true;

            return x.CompareTo((mpz_t)y) != 0;
        }

        // TODO: Optimize this by accessing memory directly
        public static bool operator !=(ulong x, mpz_t y)
        {
            if(object.ReferenceEquals(y, null))
                return true;

            return y.CompareTo((mpz_t)x) != 0;
        }

        // TODO: Optimize this by accessing memory directly
        public static bool operator !=(mpz_t x, ulong y)
        {
            if(object.ReferenceEquals(x, null))
                return true;

            return x.CompareTo((mpz_t)y) != 0;
        }

        public static bool operator !=(float x, mpz_t y)
        {
            if(object.ReferenceEquals(y, null))
                return true;

            return y.CompareTo(x) != 0;
        }

        public static bool operator !=(mpz_t x, float y)
        {
            if(object.ReferenceEquals(x, null))
                return true;

            return x.CompareTo(y) != 0;
        }

        public static bool operator !=(double x, mpz_t y)
        {
            if(object.ReferenceEquals(y, null))
                return true;

            return y.CompareTo(x) != 0;
        }

        public static bool operator !=(mpz_t x, double y)
        {
            if(object.ReferenceEquals(x, null))
                return true;

            return x.CompareTo(y) != 0;
        }

        //public static bool operator !=(decimal x, mpz_t y)
        //{
        //    if(object.ReferenceEquals(y, null))
        //        return true;

        //    return y.CompareTo(x) != 0;
        //}

        //public static bool operator !=(mpz_t x, decimal y)
        //{
        //    if(object.ReferenceEquals(x, null))
        //        return true;

        //    return x.CompareTo(y) != 0;
        //}

        public int CompareTo(object obj)
        {
            mpz_t objAsBigInt = obj as mpz_t;

            if(object.ReferenceEquals(objAsBigInt, null))
            {
                if(obj is int)
                    return this.CompareTo((int)obj);
                else if(obj is uint)
                    return this.CompareTo((uint)obj);
                else if(obj is long)
                    return this.CompareTo((long)obj);
                else if(obj is ulong)
                    return this.CompareTo((ulong)obj);
                else if(obj is double)
                    return this.CompareTo((double)obj);
                else if(obj is float)
                    return this.CompareTo((float)obj);
                else if(obj is short)
                    return this.CompareTo((short)obj);
                else if(obj is ushort)
                    return this.CompareTo((ushort)obj);
                else if(obj is byte)
                    return this.CompareTo((byte)obj);
                else if(obj is sbyte)
                    return this.CompareTo((sbyte)obj);
                //else if(obj is decimal)
                //    return this.CompareTo((decimal)obj);
                else if(obj is string)
                    return this.CompareTo(new mpz_t(obj as string));
                else
                    throw new ArgumentException("Cannot compare to " + obj.GetType());
            }

            return this.CompareTo(objAsBigInt);
        }

        public int CompareTo(mpz_t other)
        {
            return mpir.mpz_cmp(this, other);
        }

        public int CompareTo(int other)
        {
            return mpir.mpz_cmp_si(this, other);
        }

        public int CompareTo(uint other)
        {
            return mpir.mpz_cmp_ui(this, other);
        }

        // TODO: Optimize by accessing the memory directly
        public int CompareTo(long other)
        {
            mpz_t otherMpz = new mpz_t(other);
            int ret = this.CompareTo(otherMpz);
            otherMpz.Dispose();
            return ret;
        }

        // TODO: Optimize by accessing the memory directly
        public int CompareTo(ulong other)
        {
            mpz_t otherMpz = new mpz_t(other);
            int ret = this.CompareTo(otherMpz);
            otherMpz.Dispose();
            return ret;
        }

        public int CompareTo(float other)
        {
            return mpir.mpz_cmp_d(this, (double)other);
        }

        public int CompareTo(double other)
        {
            return mpir.mpz_cmp_d(this, other);
        }

        //public int CompareTo(decimal other)
        //{
        //    return mpir.mpz_cmp_d(this, (double)other);
        //}

        public int CompareAbsTo(object obj)
        {
            mpz_t objAsBigInt = obj as mpz_t;

            if(object.ReferenceEquals(objAsBigInt, null))
            {
                if(obj is int)
                    return this.CompareAbsTo((int)obj);
                else if(obj is uint)
                    return this.CompareAbsTo((uint)obj);
                else if(obj is long)
                    return this.CompareAbsTo((long)obj);
                else if(obj is ulong)
                    return this.CompareAbsTo((ulong)obj);
                else if(obj is double)
                    return this.CompareAbsTo((double)obj);
                else if(obj is float)
                    return this.CompareAbsTo((float)obj);
                else if(obj is short)
                    return this.CompareAbsTo((short)obj);
                else if(obj is ushort)
                    return this.CompareAbsTo((ushort)obj);
                else if(obj is byte)
                    return this.CompareAbsTo((byte)obj);
                else if(obj is sbyte)
                    return this.CompareAbsTo((sbyte)obj);
                //else if(obj is decimal)
                //    return this.CompareAbsTo((decimal)obj);
                else if(obj is string)
                    return this.CompareAbsTo(new mpz_t(obj as string));
                else
                    throw new ArgumentException("Cannot compare to " + obj.GetType());
            }

            return this.CompareAbsTo(objAsBigInt);
        }

        public int CompareAbsTo(mpz_t other)
        {
            return mpir.mpz_cmpabs(this, other);
        }

        public int CompareAbsTo(int other)
        {
            return mpir.mpz_cmpabs_ui(this, (uint)other);
        }

        public int CompareAbsTo(uint other)
        {
            return mpir.mpz_cmpabs_ui(this, other);
        }

        public int CompareAbsTo(long other)
        {
            return this.CompareAbsTo((mpz_t)other);
        }

        public int CompareAbsTo(ulong other)
        {
            return this.CompareAbsTo((mpz_t)other);
        }

        public int CompareAbsTo(double other)
        {
            return mpir.mpz_cmpabs_d(this, other);
        }

        //public int CompareAbsTo(decimal other)
        //{
        //    return mpir.mpz_cmpabs_d(this, (double)other);
        //}

        public static int Compare(mpz_t x, object y)
        {
            return x.CompareTo(y);
        }

        public static int Compare(object x, mpz_t y)
        {
            return -y.CompareTo(x);
        }

        public static int Compare(mpz_t x, mpz_t y)
        {
            return x.CompareTo(y);
        }

        public static int Compare(mpz_t x, int y)
        {
            return x.CompareTo(y);
        }

        public static int Compare(int x, mpz_t y)
        {
            return -y.CompareTo(x);
        }

        public static int Compare(mpz_t x, uint y)
        {
            return x.CompareTo(y);
        }

        public static int Compare(uint x, mpz_t y)
        {
            return -y.CompareTo(x);
        }

        public static int Compare(mpz_t x, long y)
        {
            return x.CompareTo(y);
        }

        public static int Compare(long x, mpz_t y)
        {
            return -y.CompareTo(x);
        }

        public static int Compare(mpz_t x, ulong y)
        {
            return x.CompareTo(y);
        }

        public static int Compare(ulong x, mpz_t y)
        {
            return -y.CompareTo(x);
        }

        public static int Compare(mpz_t x, double y)
        {
            return x.CompareTo(y);
        }

        public static int Compare(double x, mpz_t y)
        {
            return -y.CompareTo(x);
        }

        //public static int Compare(mpz_t x, decimal y)
        //{
        //    return x.CompareTo(y);
        //}

        //public static int Compare(decimal x, mpz_t y)
        //{
        //    return -y.CompareTo(x);
        //}

        public static int CompareAbs(mpz_t x, object y)
        {
            return x.CompareAbsTo(y);
        }

        public static int CompareAbs(object x, mpz_t y)
        {
            return -y.CompareAbsTo(x);
        }

        public static int CompareAbs(mpz_t x, mpz_t y)
        {
            return x.CompareAbsTo(y);
        }

        public static int CompareAbs(mpz_t x, int y)
        {
            return x.CompareAbsTo(y);
        }

        public static int CompareAbs(int x, mpz_t y)
        {
            return -y.CompareAbsTo(x);
        }

        public static int CompareAbs(mpz_t x, uint y)
        {
            return x.CompareAbsTo(y);
        }

        public static int CompareAbs(uint x, mpz_t y)
        {
            return -y.CompareAbsTo(x);
        }

        public static int CompareAbs(mpz_t x, long y)
        {
            return x.CompareAbsTo(y);
        }

        public static int CompareAbs(long x, mpz_t y)
        {
            return -y.CompareAbsTo(x);
        }

        public static int CompareAbs(mpz_t x, ulong y)
        {
            return x.CompareAbsTo(y);
        }

        public static int CompareAbs(ulong x, mpz_t y)
        {
            return -y.CompareAbsTo(x);
        }

        public static int CompareAbs(mpz_t x, double y)
        {
            return x.CompareAbsTo(y);
        }

        public static int CompareAbs(double x, mpz_t y)
        {
            return -y.CompareAbsTo(x);
        }

        //public static int CompareAbs(mpz_t x, decimal y)
        //{
        //    return x.CompareAbsTo(y);
        //}

        //public static int CompareAbs(decimal x, mpz_t y)
        //{
        //    return -y.CompareAbsTo(x);
        //}

        int IComparable.CompareTo(object obj)
        {
            return Compare(this, obj);
        }

        #endregion

        #region Casting

        public static implicit operator mpz_t(byte value)
        {
            return new mpz_t((uint)value);
        }

        public static implicit operator mpz_t(int value)
        {
            return new mpz_t(value);
        }

        public static implicit operator mpz_t(uint value)
        {
            return new mpz_t(value);
        }

        public static implicit operator mpz_t(short value)
        {
            return new mpz_t(value);
        }

        public static implicit operator mpz_t(ushort value)
        {
            return new mpz_t(value);
        }

        public static implicit operator mpz_t(long value)
        {
            return new mpz_t(value);
        }

        public static implicit operator mpz_t(ulong value)
        {
            return new mpz_t(value);
        }

        public static implicit operator mpz_t(float value)
        {
            return new mpz_t((double)value);
        }

        public static implicit operator mpz_t(double value)
        {
            return new mpz_t(value);
        }

        //public static implicit operator mpz_t(decimal value)
        //{
        //    return new mpz_t(value);
        //}

        public static explicit operator mpz_t(string value)
        {
            return new mpz_t(value, s_defaultStringBase);
        }

        public static explicit operator byte(mpz_t value)
        {
            return (byte)(uint)value;
        }

        public static explicit operator int(mpz_t value)
        {
            return mpir.mpz_get_si(value);
        }

        public static explicit operator uint(mpz_t value)
        {
            return mpir.mpz_get_ui(value);
        }

        public static explicit operator short(mpz_t value)
        {
            return (short)(int)value;
        }

        public static explicit operator ushort(mpz_t value)
        {
            return (ushort)(uint)value;
        }

        public static explicit operator long(mpz_t value)
        {
            var bytes = new byte[8];
            byte[] exportedBytes = value.ToByteArray(BitConverter.IsLittleEndian ? -1 : 1);
            int destOffset = BitConverter.IsLittleEndian ? 0 : 8 - exportedBytes.Length;

            Buffer.BlockCopy(exportedBytes, 0, bytes, destOffset, exportedBytes.Length);
            return BitConverter.ToInt64(bytes, 0);
        }

        public static explicit operator ulong(mpz_t value)
        {
            var bytes = new byte[8];
            byte[] exportedBytes = value.ToByteArray(BitConverter.IsLittleEndian ? -1 : 1);
            int destOffset = BitConverter.IsLittleEndian ? 0 : 8 - exportedBytes.Length;

            Buffer.BlockCopy(exportedBytes, 0, bytes, destOffset, exportedBytes.Length);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public static explicit operator float(mpz_t value)
        {
            return (float)(double)value;
        }

        public static explicit operator double(mpz_t value)
        {
            return mpir.mpz_get_d(value);
        }

        //public static explicit operator decimal(mpz_t value)
        //{
        //    return (decimal)(double)value;
        //}

        public static explicit operator string(mpz_t value)
        {
            return value.ToString();
        }

        #endregion

        #region Cloning

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public mpz_t Clone()
        {
            return new mpz_t(this);
        }

        #endregion

        #region Conversions

        public BigInteger ToBigInteger()
        {
            return new BigInteger(ToByteArray(-1));
        }

        public override string ToString()
        {
            return ToString((int)s_defaultStringBase);
        }

        public string ToString(uint @base)
        {
            return mpir.mpz_get_string(@base, this);
        }

        #region IConvertible Members

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return (byte)this;
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return (double)this;
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return (short)this;
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return (int)this;
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return (long)this;
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return (sbyte)this;
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return (float)this;
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return this.ToString();
        }

        object IConvertible.ToType(Type targetType, IFormatProvider provider)
        {
            if(targetType == null)
                throw new ArgumentNullException("targetType");

            if(targetType == typeof(mpz_t))
                return this;

            IConvertible value = this;

            if(targetType == typeof(sbyte))
            {
                return value.ToSByte(provider);
            }
            if(targetType == typeof(byte))
            {
                return value.ToByte(provider);
            }
            if(targetType == typeof(short))
            {
                return value.ToInt16(provider);
            }
            if(targetType == typeof(ushort))
            {
                return value.ToUInt16(provider);
            }
            if(targetType == typeof(int))
            {
                return value.ToInt32(provider);
            }
            if(targetType == typeof(uint))
            {
                return value.ToUInt32(provider);
            }
            if(targetType == typeof(long))
            {
                return value.ToInt64(provider);
            }
            if(targetType == typeof(ulong))
            {
                return value.ToUInt64(provider);
            }
            if(targetType == typeof(float))
            {
                return value.ToSingle(provider);
            }
            if(targetType == typeof(double))
            {
                return value.ToDouble(provider);
            }
            if(targetType == typeof(decimal))
            {
                return value.ToDecimal(provider);
            }
            if(targetType == typeof(string))
            {
                return value.ToString(provider);
            }
            if(targetType == typeof(object))
            {
                return value;
            }

            throw new InvalidCastException();
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return (ushort)this;
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return (uint)this;
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return (ulong)this;
        }

        #endregion

        #endregion Conversions
    }
}
