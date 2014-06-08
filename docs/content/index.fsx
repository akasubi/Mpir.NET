(*** hide ***)
#I "../../bin"

(**
Mpir.NET
========

Mpir.NET lets you use the MPIR library, which is a GMP fork for Windows,
from .NET languages. 

The current version incorporates 32- and 64-bit builds of MPIR 2.6.0.

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      EFConcurrencyModeTest can be <a href="https://nuget.org/packages/Mpir.NET">installed from NuGet</a>:
      <pre>PM> Install-Package Mpir.NET</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Mpir.NET is a wrapper around the MPIR library. It's basically a fusion of modified versions
of X-MPIR by Sergey Bochkanov and "GMP for .NET" by Emil Stefanov.

F# Example
----------

This example shows how simple Mpir.NET use looks in F#:

*)

(*** hide ***)
#r "Mpir.NET"
(** *)

open Mpir.NET

let a = new mpz_t(756749075976907490175905790287846502134I)
let b = new mpz_t(529134916478965674697197076070175107505I)
let c = a*b

(**
C# Example
----------

A short C# example:

    [lang=csharp,file=../csharp/MpirSample.cs,key=intro-sample]

Basics
------

mpz\_t is the class representing MPIR integers. If you need to use any MPIR function 
that doesn't have a corresponding mpz\_t method, it can be found in the static mpir class,
where the original MPIR/GMP function names are preserved. For example, call mpz\_addmul like so:

*)
mpir.mpz_addmul(c, a, b);
(**

Supported CPUs
--------------

Mpir.NET includes an MPIR 32-bit build for Pentium (p0 target in MPIR), and a 
64-bit build for K8. In other words, Mpir.NET runs on Pentiums and later x86 32-bit
CPUs, and all x64 64-bit CPUs. It will not run on pre-Pentium antiques or weird
stuff like Itaniums.

Copyright
---------

Mpir.NET library is licensed under LGPL, because that's the license used by the libraries
that Mpir.NET is derived from. For more information see the 
[License file][license] in the GitHub repository. 

  [gh]: https://github.com/wezeku/Mpir.NET
  [license]: https://github.com/wezeku/Mpir.NET/blob/master/LICENSE.txt
*)
