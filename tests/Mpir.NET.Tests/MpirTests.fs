module MpirTests

open System
open Mpir.NET

open NUnit.Framework


let bigNumLiteral1 = 60239597246800183089356887648914080803568478687972971429218563117893654732155483254Z
let bigNumStr1 =    "60239597246800183089356887648914080803568478687972971429218563117893654732155483254"
let bigint1 = bigint.Parse bigNumStr1


// A more readable way to concatenate arrays.
let inline private (++) (a:^T[]) (b:^T[]) = Array.append a b


type ``mpz_t - literals`` () =
    [<Test>]
    static member ``Large literal`` () =
        use z = bigNumLiteral1
        let zStr = mpir.mpz_get_string(10u, z)
        Assert.AreEqual(bigNumStr1, zStr)


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


type ``mpz_t - operations`` () =
    [<TestCase("67907490790576908375907590346925623895", "67907490790576908375907590346925623895")>]
    [<TestCase("-99943967907490790576908375907590346925623895", "99943967907490790576908375907590346925623895")>]
    [<TestCase("-1", "1")>]
    static member ``mpir.Abs``(a: string, b: string) =
        use za = new mpz_t(a)
        use zb = new mpz_t(b)
        use result = za.Abs()
        Assert.AreEqual(result, zb)

    [<TestCase("43967907490790576908375907590346925623895", "67907490790576908375907590346925623895")>]
    [<TestCase("43967907490790576908375907590346925623895", "-99943967907490790576908375907590346925623895")>]
    [<TestCase("43967907490790576908375907590346925623895", "-9994396790895")>]
    static member ``mpir.Max``(a: string, b: string) =
        use za = new mpz_t(a)
        use zb = new mpz_t(b)
        use max = mpir.Max(za, zb)
        Assert.AreEqual(za, max)

    [<TestCase("43967907490790576908375907590346925623895", "67907490790576908375907590346925623895")>]
    [<TestCase("43967907490790576908375907590346925623895", "-99943967907490790576908375907590346925623895")>]
    [<TestCase("43967907490790576908375907590346925623895", "-9994396790895")>]
    static member ``mpir.Min``(a: string, b: string) =
        use za = new mpz_t(a)
        use zb = new mpz_t(b)
        use min = mpir.Min(za, zb)
        Assert.AreEqual(zb, min)

    [<Test>]
    static member ``PowerMod with negative exponent``() =
        use za = 3Z
        use zb = 7Z

        use actual = za.PowerMod(-1, zb)
        use expected = 5Z
        Assert.AreEqual(expected, actual)
