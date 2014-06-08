module MpirTests

open System
open Mpir.NET

open NUnit.Framework


let bigNumStr1 = "60239597246800183089356887648914080803568478687972971429218563117893654732155483254"
let bigint1 = bigint.Parse bigNumStr1


// A more readable way to concatenate arrays.
let inline private (++) (a:^T[]) (b:^T[]) = Array.append a b


type ``mpz_t - import and export`` () =
    [<Test>]
    static member ``Importing BigInteger`` () =
        use z = new mpz_t(bigint1.ToByteArray(), -1)
        let zStr = mpir.mpz_get_string(10u, z)
        Assert.AreEqual(bigNumStr1, zStr)

    [<Test>]
    static member ``Exporting BigInteger`` () =
        use z = new mpz_t(bigNumStr1)
        let t = z.ToBigInteger()
        Assert.AreEqual(bigint1, t)

    [<Test>]
    static member ``Importing bigint bytes, big endian`` () =
        let a = bigint.Parse(bigNumStr1)
        use z = new mpz_t(a.ToByteArray() |> Array.rev, 1)

        let zStr = mpir.mpz_get_string(10u, z)
        Assert.AreEqual(bigNumStr1, zStr)

    [<Test>]
    static member ``Importing bigint bytes, little endian`` () =
        let a = bigint.Parse(bigNumStr1)
        use z = new mpz_t(a.ToByteArray(), -1)

        let zStr = mpir.mpz_get_string(10u, z)
        Assert.AreEqual(bigNumStr1, zStr)

    [<Test>]
    static member ``Exporting mpz, big endian`` () =
        // Make a bigint from the exported byte array and compare to that bigint's ToString()
        // rather than comparing to a bigint.Parse(bigNumStr1).ToByteArray(), as bigint.ToByteArray 
        // may or may not put a leading 0x00 first to indicate positive sign depending on MSB.
        use z = new mpz_t()
        mpir.mpz_set_str(z, bigNumStr1, 10u) |> ignore

        let bytes = z.ToByteArray(1)
        let exportStr = (bigint( [|0uy|] ++ bytes |> Array.rev )).ToString()

        Assert.AreEqual(bigNumStr1, exportStr)

    [<Test>]
    static member ``Exporting mpz, little endian`` () =
        use z = new mpz_t()
        mpir.mpz_set_str(z, bigNumStr1, 10u) |> ignore

        let bytes = z.ToByteArray(-1)
        let exportStr = (bigint(bytes)).ToString()

        Assert.AreEqual(bigNumStr1, exportStr)

    [<TestCase(18446744073709551615UL)>]
    [<TestCase( 9223372036854775807UL)>] 
    [<TestCase( 4887567363547568832UL)>] 
    [<TestCase(                   0UL)>] 
    static member ``Importing uint64, big endian`` (n : uint64) = 
        let bytes = BitConverter.GetBytes(n)
        let bigEndianBytes =
            if BitConverter.IsLittleEndian then Array.rev bytes
            else bytes
        use z = new mpz_t(bigEndianBytes, 1)

        let zStr = mpir.mpz_get_string(10u, z)
        Assert.AreEqual(n.ToString(), zStr)

    [<TestCase(18446744073709551615UL)>]
    [<TestCase( 9223372036854775807UL)>] 
    [<TestCase( 4887567363547568832UL)>] 
    [<TestCase(                   0UL)>] 
    static member ``Importing uint64, little endian`` (n : uint64) = 
        let bytes = BitConverter.GetBytes(n)
        let littleEndianBytes =
            if BitConverter.IsLittleEndian then bytes
            else Array.rev bytes
        use z = new mpz_t(littleEndianBytes, -1)

        let zStr = mpir.mpz_get_string(10u, z)
        Assert.AreEqual(n.ToString(), zStr)

type ``mpz_t - casts`` () =
    [<Test>]
    static member ``mpz_t to long`` () =
        let tstVal : int64 = 0x7F00ABCDEA007851L
        use a = new mpz_t(tstVal)
        let b = (int64 a)
        Assert.AreEqual(tstVal, b)

    [<Test>]
    static member ``mpz_t to ulong`` () =
        let tstVal : uint64 = 0xFF00ABCDEA007851UL
        use a = new mpz_t(tstVal)
        let b = (uint64 a)
        Assert.AreEqual(tstVal, b)
