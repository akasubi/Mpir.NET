using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Disable warning about missing XML comments.
#pragma warning disable 1591

namespace Mpir.NET
{
    public class mpq_t : IDisposable
    {
        #region Data

        public IntPtr val;
        private bool disposed = false;
        
        #endregion

        #region Creation and destruction

        public mpq_t() { val = mpir.mpq_init(); }

        public mpq_t(string str) : this(str, 10u) {}

        public mpq_t(string str, uint _base)
        {
            mpir.mpq_set_str(this, str, _base);
        }

        ~mpq_t()
        {
            Dispose(false);
        }
        
        public void Dispose() 
        {
            Dispose(true);
            GC.SuppressFinalize(this);      
        }
        
        protected virtual void Dispose(bool disposing)
        {
            // There are no managed resources, so <disposing> is not used.
            if (!disposed) {
                // dispose unmanaged resources
                mpir.mpq_clear(this);
                disposed = true;   
            }
        }

        #endregion
    }
}
