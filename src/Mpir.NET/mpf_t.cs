using System;
using System.Collections.Generic;

#pragma warning disable 1591

namespace Mpir.NET
{
    public class mpf_t : IDisposable
    {
        #region Data

        public IntPtr val;
        private bool disposed = false;
        
        #endregion

        #region Creation and destruction

        public mpf_t(mpf_t op)                { val = mpir.mpf_init_set(op);           }
        public mpf_t(int op)                  { val = mpir.mpf_init_set_si(op);        }
        public mpf_t(uint op)                 { val = mpir.mpf_init_set_ui(op);        }
        public mpf_t(double op)               { val = mpir.mpf_init_set_d(op);         }
        public mpf_t(string s, uint _base)    { val = mpir.mpf_init_set_str(s, _base); }
        public mpf_t(string s) : this(s, 10u) {}

        // Initialization with mpf_init2 should not be confused with mpf_t construction
        // from a uint. Thus, so we use a static construction function instead, and add
        // the dummy type init2_type to enable us to write a ctor with a unique signature.
        public static mpf_t init2(uint prec)  { return new mpf_t(init2_type.init2, prec); }
        private enum init2_type { init2 }
        private mpf_t(init2_type dummy, uint arg) 
        { 
            val = mpir.mpf_init2(arg);
        }

        ~mpf_t()
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
                mpir.mpf_clear(this);
                disposed = true;   
            }
        }

        #endregion
    }
}
