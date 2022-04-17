using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    class GDI32
    {
        [DllImport("gdi32", SetLastError = true)]
        public static extern bool BitBlt(SafeHandle hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, SafeHandle hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);

        public abstract class SafeObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            [DllImport("gdi32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool DeleteObject(IntPtr hObject);

            protected SafeObjectHandle(bool ownsHandle) : base(ownsHandle)
            {
            }

            protected override bool ReleaseHandle()
            {
                return DeleteObject(handle);
            }
        }
        public class SafeDibSectionHandle : SafeObjectHandle
        {
            /// <summary>
            /// Needed for marshalling return values
            /// </summary>
            [SecurityCritical]
            public SafeDibSectionHandle() : base(true)
            {
            }

            [SecurityCritical]
            public SafeDibSectionHandle(IntPtr preexistingHandle) : base(true)
            {
                SetHandle(preexistingHandle);
            }
        }

        [DllImport("gdi32", SetLastError = true)]
        public static extern SafeDibSectionHandle CreateDIBSection(SafeHandle hdc, ref BITMAPINFOHEADERV5 bmi, uint usage, out IntPtr bits, IntPtr hSection, uint dwOffset);

        [DllImport("gdi32", SetLastError = true)]
        public static extern SafeCompatibleDCHandle CreateCompatibleDC(SafeHandle hDC);

        public abstract class SafeDCHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            protected SafeDCHandle(bool ownsHandle) : base(ownsHandle)
            {
            }
        }

        public class SafeSelectObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            [DllImport("gdi32", SetLastError = true)]
            private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

            private readonly SafeHandle _hdc;

            /// <summary>
            /// Needed for marshalling return values
            /// </summary>
            [SecurityCritical]
            public SafeSelectObjectHandle() : base(true)
            {
            }

            [SecurityCritical]
            public SafeSelectObjectHandle(SafeDCHandle hdc, SafeHandle newHandle) : base(true)
            {
                _hdc = hdc;
                SetHandle(SelectObject(hdc.DangerousGetHandle(), newHandle.DangerousGetHandle()));
            }

            protected override bool ReleaseHandle()
            {
                SelectObject(_hdc.DangerousGetHandle(), handle);
                return true;
            }
        }

        public class SafeCompatibleDCHandle : SafeDCHandle
        {
            [DllImport("gdi32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool DeleteDC(IntPtr hDC);

            /// <summary>
            /// Needed for marshalling return values
            /// </summary>
            [SecurityCritical]
            public SafeCompatibleDCHandle() : base(true)
            {
            }

            [SecurityCritical]
            public SafeCompatibleDCHandle(IntPtr preexistingHandle) : base(true)
            {
                SetHandle(preexistingHandle);
            }

            public SafeSelectObjectHandle SelectObject(SafeHandle newHandle)
            {
                return new SafeSelectObjectHandle(this, newHandle);
            }

            protected override bool ReleaseHandle()
            {
                return DeleteDC(handle);
            }
        }

        public class SafeWindowDcHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            [DllImport("user32", SetLastError = true)]
            private static extern IntPtr GetWindowDC(IntPtr hWnd);

            [DllImport("user32", SetLastError = true)]
            private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

            private readonly IntPtr _hWnd;

            /// <summary>
            /// Needed for marshalling return values
            /// </summary>
            public SafeWindowDcHandle() : base(true)
            {
            }

            [SecurityCritical]
            public SafeWindowDcHandle(IntPtr hWnd, IntPtr preexistingHandle) : base(true)
            {
                _hWnd = hWnd;
                SetHandle(preexistingHandle);
            }

            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
            protected override bool ReleaseHandle()
            {
                bool returnValue = ReleaseDC(_hWnd, handle);
                return returnValue;
            }

            /// <summary>
            /// Creates a DC as SafeWindowDcHandle for the whole of the specified hWnd
            /// </summary>
            /// <param name="hWnd">IntPtr</param>
            /// <returns>SafeWindowDcHandle</returns>
            public static SafeWindowDcHandle FromWindow(IntPtr hWnd)
            {
                if (hWnd == IntPtr.Zero)
                {
                    return null;
                }

                var hDcDesktop = GetWindowDC(hWnd);
                return new SafeWindowDcHandle(hWnd, hDcDesktop);
            }

            public static SafeWindowDcHandle FromDesktop()
            {
                IntPtr hWndDesktop = User32.GetDesktopWindow();
                IntPtr hDCDesktop = GetWindowDC(hWndDesktop);
                return new SafeWindowDcHandle(hWndDesktop, hDCDesktop);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CIEXYZ
        {
            public uint ciexyzX; //FXPT2DOT30
            public uint ciexyzY; //FXPT2DOT30
            public uint ciexyzZ; //FXPT2DOT30

            public CIEXYZ(uint FXPT2DOT30)
            {
                ciexyzX = FXPT2DOT30;
                ciexyzY = FXPT2DOT30;
                ciexyzZ = FXPT2DOT30;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RGBQUAD
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CIEXYZTRIPLE
        {
            public CIEXYZ ciexyzRed;
            public CIEXYZ ciexyzGreen;
            public CIEXYZ ciexyzBlue;
        }

        public enum BI_COMPRESSION : uint
        {
            BI_RGB = 0, // Uncompressed
            BI_RLE8 = 1, // RLE 8BPP
            BI_RLE4 = 2, // RLE 4BPP

            BI_BITFIELDS =
                3, // Specifies that the bitmap is not compressed and that the color table consists of three DWORD color masks that specify the red, green, and blue components, respectively, of each pixel. This is valid when used with 16- and 32-bpp bitmaps.
            BI_JPEG = 4, // Indicates that the image is a JPEG image.
            BI_PNG = 5 // Indicates that the image is a PNG image.
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct BITMAPINFOHEADERV4
        {
            [FieldOffset(0)] public uint biSize;
            [FieldOffset(4)] public int biWidth;
            [FieldOffset(8)] public int biHeight;
            [FieldOffset(12)] public ushort biPlanes;
            [FieldOffset(14)] public ushort biBitCount;
            [FieldOffset(16)] public BI_COMPRESSION biCompression;
            [FieldOffset(20)] public uint biSizeImage;
            [FieldOffset(24)] public int biXPelsPerMeter;
            [FieldOffset(28)] public int biYPelsPerMeter;
            [FieldOffset(32)] public uint biClrUsed;
            [FieldOffset(36)] public uint biClrImportant;
            [FieldOffset(40)] public uint bV4RedMask;
            [FieldOffset(44)] public uint bV4GreenMask;
            [FieldOffset(48)] public uint bV4BlueMask;
            [FieldOffset(52)] public uint bV4AlphaMask;
            [FieldOffset(56)] public uint bV4CSType;
            [FieldOffset(60)] public CIEXYZTRIPLE bV4Endpoints;
            [FieldOffset(96)] public uint bV4GammaRed;
            [FieldOffset(100)] public uint bV4GammaGreen;
            [FieldOffset(104)] public uint bV4GammaBlue;

            public const int DIB_RGB_COLORS = 0;

            public BITMAPINFOHEADERV4(int width, int height, ushort bpp)
            {
                biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADERV4)); // BITMAPINFOHEADER < DIBV5 is 40 bytes
                biPlanes = 1; // Should allways be 1
                biCompression = BI_COMPRESSION.BI_RGB;
                biWidth = width;
                biHeight = height;
                biBitCount = bpp;
                biSizeImage = (uint)(width * height * (bpp >> 3));
                biXPelsPerMeter = 0;
                biYPelsPerMeter = 0;
                biClrUsed = 0;
                biClrImportant = 0;

                // V4
                bV4RedMask = (uint)255 << 16;
                bV4GreenMask = (uint)255 << 8;
                bV4BlueMask = 255;
                bV4AlphaMask = (uint)255 << 24;
                bV4CSType = 0x73524742; // LCS_sRGB
                bV4Endpoints = new CIEXYZTRIPLE
                {
                    ciexyzBlue = new CIEXYZ(0),
                    ciexyzGreen = new CIEXYZ(0),
                    ciexyzRed = new CIEXYZ(0)
                };
                bV4GammaRed = 0;
                bV4GammaGreen = 0;
                bV4GammaBlue = 0;
            }

            public bool IsDibV4
            {
                get
                {
                    uint sizeOfBMI = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADERV4));
                    return biSize >= sizeOfBMI;
                }
            }
            public bool IsDibV5
            {
                get
                {
                    uint sizeOfBMI = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADERV5));
                    return biSize >= sizeOfBMI;
                }
            }

            public uint OffsetToPixels
            {
                get
                {
                    if (biCompression == BI_COMPRESSION.BI_BITFIELDS)
                    {
                        // Add 3x4 bytes for the bitfield color mask
                        return biSize + 3 * 4;
                    }

                    return biSize;
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct BITMAPINFOHEADERV5
        {
            [FieldOffset(0)] public uint biSize;
            [FieldOffset(4)] public int biWidth;
            [FieldOffset(8)] public int biHeight;
            [FieldOffset(12)] public ushort biPlanes;
            [FieldOffset(14)] public ushort biBitCount;
            [FieldOffset(16)] public BI_COMPRESSION biCompression;
            [FieldOffset(20)] public uint biSizeImage;
            [FieldOffset(24)] public int biXPelsPerMeter;
            [FieldOffset(28)] public int biYPelsPerMeter;
            [FieldOffset(32)] public uint biClrUsed;
            [FieldOffset(36)] public uint biClrImportant;
            [FieldOffset(40)] public uint bV4RedMask;
            [FieldOffset(44)] public uint bV4GreenMask;
            [FieldOffset(48)] public uint bV4BlueMask;
            [FieldOffset(52)] public uint bV4AlphaMask;
            [FieldOffset(56)] public uint bV4CSType;
            [FieldOffset(60)] public CIEXYZTRIPLE bV4Endpoints;
            [FieldOffset(96)] public uint bV4GammaRed;
            [FieldOffset(100)] public uint bV4GammaGreen;
            [FieldOffset(104)] public uint bV4GammaBlue;
            [FieldOffset(108)] public uint bV5Intent; // Rendering intent for bitmap 
            [FieldOffset(112)] public uint bV5ProfileData;
            [FieldOffset(116)] public uint bV5ProfileSize;
            [FieldOffset(120)] public uint bV5Reserved;

            public const int DIB_RGB_COLORS = 0;

            public BITMAPINFOHEADERV5(int width, int height, ushort bpp)
            {
                biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADERV5)); // BITMAPINFOHEADER < DIBV5 is 40 bytes
                biPlanes = 1; // Should allways be 1
                biCompression = BI_COMPRESSION.BI_RGB;
                biWidth = width;
                biHeight = height;
                biBitCount = bpp;
                biSizeImage = (uint)(width * height * (bpp >> 3));
                biXPelsPerMeter = 0;
                biYPelsPerMeter = 0;
                biClrUsed = 0;
                biClrImportant = 0;

                // V4
                bV4RedMask = (uint)255 << 16;
                bV4GreenMask = (uint)255 << 8;
                bV4BlueMask = 255;
                bV4AlphaMask = (uint)255 << 24;
                bV4CSType = 0x73524742; // LCS_sRGB
                bV4Endpoints = new CIEXYZTRIPLE
                {
                    ciexyzBlue = new CIEXYZ(0),
                    ciexyzGreen = new CIEXYZ(0),
                    ciexyzRed = new CIEXYZ(0)
                };
                bV4GammaRed = 0;
                bV4GammaGreen = 0;
                bV4GammaBlue = 0;
                // V5
                bV5Intent = 4;
                bV5ProfileData = 0;
                bV5ProfileSize = 0;
                bV5Reserved = 0;
            }

            public bool IsDibV4
            {
                get
                {
                    uint sizeOfBMI = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADERV4));
                    return biSize >= sizeOfBMI;
                }
            }
            public bool IsDibV5
            {
                get
                {
                    uint sizeOfBMI = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADERV5));
                    return biSize >= sizeOfBMI;
                }
            }

            public uint OffsetToPixels
            {
                get
                {
                    if (biCompression == BI_COMPRESSION.BI_BITFIELDS)
                    {
                        // Add 3x4 bytes for the bitfield color mask
                        return biSize + 3 * 4;
                    }

                    return biSize;
                }
            }
        }
    }
}
