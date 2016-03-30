/*
Copyright 2010 Sergey Bochkanov.

The X-MPIR is free software; you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation; either version 3 of the License, or (at your
option) any later version.

The X-MPIR is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
License for more details.

You should have received a copy of the GNU Lesser General Public License
along with the X-MPIR; see the file COPYING.LIB.  If not, write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
MA 02110-1301, USA.
*/

/*
Modifications by John Reynolds, to provide disposal of unmanaged resources,
binary import/export functions etc.
*/    

using System;
using System.Reflection;
using System.Runtime.InteropServices;

// Disable warning about missing XML comments.
#pragma warning disable 1591

namespace Mpir.NET 
{

    public static partial class mpir
    {
        // <dummy> makes sure that <hxmpir> is initialized before it's used in the 
        // field assignments later in this file. hxmpir is declared in xmpir.cs,
        // making sure it's initialized before it's used in that file.
        // So, regardless of whether the CLR initializes the static fields of
        // mpir.cs or xmpir.cs first, <hxmpir> should always be initialized first.
        private static IntPtr dummy = initialize_hxmpir();

        private static IntPtr initialize_hxmpir()
        {
            if (hxmpir == IntPtr.Zero)
                hxmpir = LoadLibrarySafe(GetXMPIRLibraryPath());
            return hxmpir;
        }

        #region Static mpz_t functions.

        /// Returns the largest number of a and b.
        public static mpz_t Max(mpz_t a, mpz_t b)
        {
            return a > b ? a : b;
        }

        /// Returns the smallest number of a and b.
        public static mpz_t Min(mpz_t a, mpz_t b)
        {
            return a < b ? a : b;
        }

        #endregion

        #region Wrappers for dynamic loading functions

        public static string GetOSString()
        {
            if( System.IO.Path.DirectorySeparatorChar=='/' )
                return "linux";
            return "windows";
        }
        public static string LocateLibrary(string name)
        {
            //
            // try to locate file using one of two methods:
            // * GetExecutingAssembly().CodeBase property for assemblies NOT in the GAC
            // * GetEntryAssembly().CodeBase property (if previous attempt failed)
            //
            string libpath = "";
            if( Assembly.GetExecutingAssembly().CodeBase!="" )
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                libpath = System.IO.Path.GetDirectoryName(path);
                if( !System.IO.File.Exists(libpath+System.IO.Path.DirectorySeparatorChar+name) )
                    libpath = "";
            }
            if( libpath=="" )
                if( Assembly.GetEntryAssembly()!=null )
                {
                    string codeBase = Assembly.GetEntryAssembly().CodeBase;
                    UriBuilder uri = new UriBuilder(codeBase);
                    string path = Uri.UnescapeDataString(uri.Path);
                    libpath = System.IO.Path.GetDirectoryName(path);
                    if( !System.IO.File.Exists(libpath+System.IO.Path.DirectorySeparatorChar+name) )
                        libpath = "";
                }
            if( libpath=="" )
                throw new System.Exception("MPIR: can't determine path to the "+name);
            return libpath+System.IO.Path.DirectorySeparatorChar+name;
        }
        public static string GetXMPIRLibraryPath()
        {
            string os = GetOSString();
            string libname = "";
            if( os=="linux" )
                libname = "xmpir.so";
            if( os=="windows" )
                libname = "xmpir"+(IntPtr.Size*8).ToString()+".dll";
            if( libname=="" )
                throw new System.Exception("MPIR: unknown OS - '"+os+"'");
            return LocateLibrary(libname);
        }
        private static void HandleError(int ErrorCode)
        {
            //Environment.Exit(-1);
            if( ErrorCode==1 )
                throw new System.OutOfMemoryException("MPIR: out of memory!");
            if( ErrorCode==2 )
                throw new System.Exception("MPIR: division by zero!");
            if( ErrorCode==3 )
                throw new System.Exception("MPIR: 64-bit index in 32-bit mode!");
            throw new System.Exception("MPIR: unknown error!");
        }

        private static IntPtr LoadLibrarySafe(string name)
        {
            IntPtr hResult = Mpir.NET.MpirDynamicLoader.LoadLibrarySafe(name);
            if( hResult.Equals(IntPtr.Zero) )
                throw new System.Exception("MPIR: unable to dlopen('"+name+"')");
            return hResult;
        }
        private static IntPtr GetProcAddressSafe(IntPtr hLib, string name)
        {
            IntPtr hResult = Mpir.NET.MpirDynamicLoader.GetProcAddressSafe(hLib, name);
            if( hResult.Equals(IntPtr.Zero) )
                throw new System.Exception("MPIR: unable to dlsym('"+name+"')");
            return hResult;
        }

        #endregion

        //
        // memory management functions
        //
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int __xmpir_malloc(out IntPtr p, int size);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int __xmpir_free(IntPtr p);
        private static IntPtr __ptr__xmpir_malloc = GetProcAddressSafe(hxmpir, "xmpir_malloc");
        private static IntPtr __ptr__xmpir_free = GetProcAddressSafe(hxmpir, "xmpir_free");
        private static __xmpir_malloc xmpir_malloc = (__xmpir_malloc)Marshal.GetDelegateForFunctionPointer(__ptr__xmpir_malloc, typeof(__xmpir_malloc));
        private static __xmpir_free xmpir_free = (__xmpir_free)Marshal.GetDelegateForFunctionPointer(__ptr__xmpir_free, typeof(__xmpir_free));

        #region Import and export functions

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void __Mpir_internal_mpz_import(IntPtr rop, uint count, int order, uint size, int endian, uint nails, void* op);
        private static IntPtr __ptr__Mpir_internal_mpz_import = GetProcAddressSafe(hxmpir, "Mpir_internal_mpz_import");
        private static __Mpir_internal_mpz_import Mpir_internal_mpz_import = (__Mpir_internal_mpz_import)Marshal.GetDelegateForFunctionPointer(__ptr__Mpir_internal_mpz_import, typeof(__Mpir_internal_mpz_import));
        public static unsafe void Mpir_mpz_import(mpz_t rop, uint count, int order, uint size, int endian, uint nails, byte[] op)
        {
            fixed (void* srcPtr = op) {
                mpir.Mpir_internal_mpz_import(rop.val, count, order, size, endian, nails, srcPtr);
            }
        }
        public static unsafe void Mpir_mpz_import_by_offset(mpz_t rop, int startOffset, int endOffset, int order, uint size, int endian, uint nails, byte[] op)
        {
            fixed (byte* srcPtr = op) {
                mpir.Mpir_internal_mpz_import(rop.val, (uint)(endOffset-startOffset+1), order, size, endian, nails, srcPtr+startOffset);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate IntPtr __Mpir_internal_mpz_export(void *rop, uint *countp, int order, uint size, int endian, uint nails, IntPtr op);
        private static IntPtr __ptr__Mpir_internal_mpz_export = GetProcAddressSafe(hxmpir, "Mpir_internal_mpz_export");
        private static __Mpir_internal_mpz_export Mpir_internal_mpz_export = (__Mpir_internal_mpz_export)Marshal.GetDelegateForFunctionPointer(__ptr__Mpir_internal_mpz_export, typeof(__Mpir_internal_mpz_export));
        public static unsafe byte[] Mpir_mpz_export(int order, uint size, int endian, uint nails, mpz_t op)
        {
            uint bufSize = mpir.mpz_sizeinbase(op, 256);
            var  destBuf = new byte[bufSize];
            fixed (void* destPtr = destBuf) {
                // null countp argument, because we already know how large the result will be.
                mpir.Mpir_internal_mpz_export(destPtr, null, order, size, endian, nails, op.val);
            }
            return destBuf;
        }
        #endregion
    }
}
