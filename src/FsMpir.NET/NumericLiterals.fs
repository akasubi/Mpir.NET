namespace Mpir.NET

[<AutoOpen>]
module NumericLiteralZ =
    let FromZero () = mpz_t.Zero
    let FromOne ()  = mpz_t.One
    let FromInt32 (n : int32)   = new mpz_t(n)
    let FromInt64 (n : int64)   = new mpz_t(n)
    let FromString (s : string) = new mpz_t(s)
