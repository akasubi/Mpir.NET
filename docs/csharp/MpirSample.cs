using System;
using System.Text;

// [intro-sample]
using Mpir.NET;

class MpirSample
{
    static void MpirCalcs()
    {
        mpz_t a = new mpz_t(12345678901234567890);
        mpz_t b = new mpz_t(9876543210987654321);
        mpz_t c = a * b;
        System.Console.WriteLine("{0}", c.ToString());
    }
}
// [/intro-sample]

class Foo
{
    static void Bar()
    {
        // [using-sample]
        using (mpz_t a = new mpz_t(12345678901234567890))
        using (mpz_t b = new mpz_t(9876543210987654321)) 
        using (mpz_t c = a * b)
        {
            System.Console.WriteLine("{0}", c.ToString());
        }
        // [/using-sample]
    }
}
