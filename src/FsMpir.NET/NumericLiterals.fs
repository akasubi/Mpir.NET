namespace Mpir.NET

[<AutoOpen>]
module NumericLiteralZ =
    let FromZero () = new mpz_t(0)
    let FromOne ()  = new mpz_t(1)
    let FromInt32 (n : int32)   = new mpz_t(n)
    let FromInt64 (n : int64)   = new mpz_t(n)
    let FromString (s : string) = new mpz_t(s)
