using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp1;
using static WinFormsApp1.GDI32;

namespace WinFormsApp1
{
    public delegate bool CallBackPtr(IntPtr hwnd, int lParam);
    public class User32Stuff
    {
        public static bool GetWindowRect(IntPtr hwnd, out Rectangle rectangle)
        {
            var windowInfo = new WindowInfo();
            // Get the Window Info for this window
            bool result = GetWindowInfo(hwnd, ref windowInfo);
            rectangle = result ? windowInfo.rcWindow.ToRectangle() : Rectangle.Empty;
            return result;
        }

        [DllImport("user32", SetLastError = true)]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32", SetLastError = true)]
        private static extern int EnumWindows(CallBackPtr callPtr, int lPar);

        [DllImport("user32", SetLastError = true)]
        public static extern bool GetWindowInfo(IntPtr hWnd, ref WindowInfo pwi);

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessage(int hWnd, int Msg, int wparam, int lparam);

        const int WM_GETTEXT = 0x000D;
        const int WM_GETTEXTLENGTH = 0x000E;

        public static string GetControlText(IntPtr hWnd)
        {
            try
            {
                // Get the size of the string required to hold the window title (including trailing null.) 
                Int32 titleSize = SendMessage((int)hWnd, WM_GETTEXTLENGTH, 0, 0).ToInt32();

                // If titleSize is 0, there is no title so return an empty string (or null)
                if (titleSize == 0)
                    return String.Empty;

                StringBuilder title = new StringBuilder(titleSize + 1);
                SendMessage(hWnd, (int)WM_GETTEXT, title.Capacity, title);
                return title.ToString();
            } catch (Exception e)
            {
                return "";
            }
        }

        public WindowInfo GetWindowInfo(IntPtr hwnd)
        {
            WindowInfo ret = new WindowInfo();
            GetWindowInfo(hwnd, ref ret);
            return ret;
        }

        public static List<Tuple<IntPtr, string> > ListAllWindows()
        {
            List<Tuple<IntPtr, string>> ret = new();
            Console.WriteLine(">> EnumWindows");

            EnumWindows(new CallBackPtr((hwnd, lParam) =>
            {
                string title = GetControlText(hwnd);
                if (title != "")
                {
                    Console.WriteLine(title);
                    ret.Add(new Tuple<IntPtr, string>(hwnd, title));
                }

                return true;
            }), 0);

            return ret;
        }

        public static Bitmap CaptureWindow(IntPtr hwnd)
        {
            SafeWindowDcHandle desktopDcHandle = SafeWindowDcHandle.FromWindow(hwnd);
            SafeCompatibleDCHandle safeCompatibleDcHandle = CreateCompatibleDC(desktopDcHandle);
            Rectangle rectangle = new Rectangle();
            User32Stuff.GetWindowRect(hwnd, out rectangle);
            BITMAPINFOHEADERV5 bmi = new BITMAPINFOHEADERV5(rectangle.Width, rectangle.Height, 24);
            Win32.SetLastError(0);
            SafeDibSectionHandle safeDibSectionHandle = GDI32.CreateDIBSection(desktopDcHandle, ref bmi,
                BITMAPINFOHEADERV5.DIB_RGB_COLORS, out _, IntPtr.Zero, 0);
            safeCompatibleDcHandle.SelectObject(safeDibSectionHandle);
            bool bbret = GDI32.BitBlt(safeCompatibleDcHandle, 0, 0, 
                rectangle.Width, rectangle.Height, desktopDcHandle, 
                0, 0,
                //CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
                CopyPixelOperation.SourceCopy);
            Console.WriteLine(string.Format("BitBlt={0}", bbret));
            Bitmap tmpBitmap = Image.FromHbitmap(safeDibSectionHandle.DangerousGetHandle());

            return tmpBitmap;

            //return null;
        }

        public static Bitmap CaptureDesktop()
        {
            SafeWindowDcHandle desktopDcHandle = SafeWindowDcHandle.FromDesktop();
            SafeCompatibleDCHandle safeCompatibleDcHandle = CreateCompatibleDC(desktopDcHandle);
            Rectangle rectangle = GetScreenBounds();
            BITMAPINFOHEADERV5 bmi = new BITMAPINFOHEADERV5(rectangle.Width, rectangle.Height, 24);
            Win32.SetLastError(0);
            SafeDibSectionHandle safeDibSectionHandle = GDI32.CreateDIBSection(desktopDcHandle, ref bmi,
                BITMAPINFOHEADERV5.DIB_RGB_COLORS, out _, IntPtr.Zero, 0);
            safeCompatibleDcHandle.SelectObject(safeDibSectionHandle);
            GDI32.BitBlt(safeCompatibleDcHandle, 0, 0,
                rectangle.Width, rectangle.Height, desktopDcHandle,
                rectangle.X, rectangle.Y,
                CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
            Bitmap tmpBitmap = Image.FromHbitmap(safeDibSectionHandle.DangerousGetHandle());

            return tmpBitmap;
        }

        public static Rectangle GetScreenBounds()
        {
            int left = 0, top = 0, bottom = 0, right = 0;
            foreach (Screen screen in Screen.AllScreens)
            {
                left = Math.Min(left, screen.Bounds.X);
                top = Math.Min(top, screen.Bounds.Y);
                int screenAbsRight = screen.Bounds.X + screen.Bounds.Width;
                int screenAbsBottom = screen.Bounds.Y + screen.Bounds.Height;
                right = Math.Max(right, screenAbsRight);
                bottom = Math.Max(bottom, screenAbsBottom);
            }

            return new Rectangle(left, top, (right + Math.Abs(left)), (bottom + Math.Abs(top)));
        }

    }
}
