using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    class Win32
    {
        [DllImport("kernel32.dll")]
        public static extern void SetLastError(uint dwErrCode);
    }
}
