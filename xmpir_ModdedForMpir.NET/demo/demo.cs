using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
class MyClass
{
    [STAThread]
    static void Main(string[] args)
    {
        mpir.mpz_t f = mpir.mpz_init_set_ui(1);
        int i;
        for(i=2; i<=30; i++)
            mpir.mpz_mul_si(f, f, i);
        System.Console.Write("30! = ");
        System.Console.WriteLine(mpir.mpz_get_string(10,f));
        mpir.mpz_clear(f);        
        
    }
}
