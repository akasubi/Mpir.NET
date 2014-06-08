using System;
using System.Runtime.InteropServices;

namespace Mpir.NET
{
    class MpirDynamicLoader
    {
        // Windows imports
        [DllImport("kernel32")]
        private extern static IntPtr LoadLibrary(string libraryName);
        [DllImport("kernel32", CharSet=CharSet.Ansi)]
        private extern static IntPtr GetProcAddress(IntPtr hwnd, string procedureName);

        // Linux imports
        const int RTLD_NOW = 2;
        [DllImport("libdl.so")]
        static extern IntPtr dlopen(string filename, int flags);
        [DllImport("libdl.so")]
        static extern IntPtr dlsym(IntPtr handle, string symbol);

        public static bool IsWindows()
        {
            return System.IO.Path.DirectorySeparatorChar == '\\';
        }

        public static IntPtr LoadLibrarySafe(string name)
        {
            if (IsWindows())
                return LoadLibrary(name);
            else
                return dlopen(name, RTLD_NOW);
        }

        public static IntPtr GetProcAddressSafe(IntPtr hLib, string name)
        {
            if (IsWindows())
                return GetProcAddress(hLib, name);
            else
                return dlsym(hLib, name);
        }
    }
}
