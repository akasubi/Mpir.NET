(*** hide ***)
#I "../../bin"

(**
Mpir.NET
========

Mpir.NET lets you use the [MPIR library](http://mpir.org), which is a GMP fork for Windows,
from .NET languages. 

The current version incorporates 32- and 64-bit builds of MPIR 2.6.0.

Installation
------------

To use Mpir.NET, first <a href="https://nuget.org/packages/Mpir.NET">install it from NuGet</a>:

<pre>PM> Install-Package Mpir.NET</pre>

This will place 2 unreferenced native DLLs in your project, as shown below:

![](img/solutionexplorer01.png)

Now, right click xmpir64.dll in the solution explorer and select "Properties". 
Change "Copy to Output Directory" from "Do not copy" to "Copy always":

![](img/xmpir64dll_properties01.png)

Repeat this for xmpir32.dll.

If you don't change the copy options for the xmpirXX DLLs, you will get a 
System.TypeInitializationException when you use Mpir.NET.

(Unfortunately, you can't instruct NuGet to place unreferenced DLLs in your project's output 
directory automatically, which is why you have to do this manually after adding the 
Mpir.NET NuGet package.)

F# Example
----------

The type representing MPIR integers is mpz\_t. In F#, you can write literals suffixed with Z, to 
create mpz\_t objects, as in this simple example:

*)

(*** hide ***)
#r "Mpir.NET"
#r "FsMpir.NET"
(** *)

open Mpir.NET

let a = 756749075976907490175905790287846502134Z
let b = 529134916478965674697197076070175107505Z
let c = a*b
printfn "%O" c

(**
C# Example
----------

A short C# example:

    [lang=csharp,file=../csharp/MpirSample.cs,key=intro-sample]

Basics
------

As mentioned before, mpz\_t is the class that represents MPIR integers. If you need to use 
any MPIR function that doesn't have a corresponding mpz\_t method, it can be found in the 
static mpir class, where the original MPIR/GMP function names are preserved. For example, 
call mpz\_addmul like so:

*)
mpir.mpz_addmul(c, a, b);
(**

Supported CPUs
--------------

Mpir.NET includes an MPIR 32-bit build for Pentium (p0 target in MPIR), and a 
64-bit build for K8. In other words, Mpir.NET runs on Pentiums and later x86 32-bit
CPUs, and all x64 64-bit CPUs. It will not run on pre-Pentium antiques or weird
stuff like Itaniums.

Origins
-------

Mpir.NET is basically a fusion of modified versions
of X-MPIR by Sergey Bochkanov and "GMP for .NET" by Emil Stefanov.

Copyright
---------

Mpir.NET library is licensed under LGPL, because that's the license used by the libraries
that Mpir.NET is derived from. For more information see the 
[License file][license] in the GitHub repository. 

  [gh]: https://github.com/wezeku/Mpir.NET
  [license]: https://github.com/wezeku/Mpir.NET/blob/master/LICENSE.txt
*)
