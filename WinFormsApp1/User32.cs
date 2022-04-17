using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    class User32
    {
        [DllImport("user32", SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();
    }
}
