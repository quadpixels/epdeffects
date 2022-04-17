using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Runtime.InteropServices;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        private List<Tuple<IntPtr, string>> _allWindows;

        User32Stuff user32Stuff;

        public Form1()
        {
            AllocConsole();
            InitializeComponent();
            user32Stuff = new User32Stuff();            
            notifyIcon1.Visible = true;
            this.FormBorderStyle = FormBorderStyle.None;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<Tuple<IntPtr, string>> aw = User32Stuff.ListAllWindows();
            _allWindows = aw;
            comboBox1.Items.Clear();
            for (int i=0; i<aw.Count; i++)
            {
                comboBox1.Items.Add(aw[i].Item2);
            }

            int w = pictureBox1.Width, h = pictureBox1.Height;
            Bitmap b = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            for (int y=0; y<h; y++)
            {
                for (int x=0; x<w; x++)
                {
                    byte r = (byte)(x * 255.0 / w);
                    byte g = (byte)(y * 255.0 / h);
                    Color c = Color.FromArgb(r, g, 0);
                    b.SetPixel(x, y, c);
                }
            }
            pictureBox1.Image = (Image)b;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int idx = comboBox1.SelectedIndex;
            if (idx < 0 || idx >= _allWindows.Count) return;

            IntPtr hwnd = _allWindows[idx].Item1;

            string title = _allWindows[idx].Item2;
            Console.WriteLine(string.Format("Capturing window {0}", title));
            Bitmap bmp = User32Stuff.CaptureWindow(hwnd);
            Console.WriteLine(bmp);

            try
            {
                pictureBox1.Image = ImageHelper.ResizeImage((Image)bmp, pictureBox1.Width, pictureBox1.Height);

                Rectangle rectangle = new();
                User32Stuff.GetWindowRect(hwnd, out rectangle);

                label2.Text = string.Format("Handle: {0:X}\nRect: {1},{2} {3}x{4}", hwnd,
                    rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Bitmap img = User32Stuff.CaptureDesktop();
            try
            {
                //pictureBox1.Image = ImageUtil.ResizeImage(img, pictureBox1.Width, pictureBox1.Height);
                pictureBox1.Image = (Image)img;
                label1.Text = string.Format("Desktop handle: {0:X}\nsize: {1}x{2}",
                    User32.GetDesktopWindow(),
                    img.Width, img.Height);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            int HTTRANSPARENT = -1;
            const int RESIZE_HANDLE_SIZE = 10;

            switch (m.Msg)
            {
                case WM_NCHITTEST:
                    base.WndProc(ref m);

                    if ((int)m.Result == 0x01/*HTCLIENT*/)
                    {
                        Point screenPoint = new Point(m.LParam.ToInt32());
                        Point clientPoint = this.PointToClient(screenPoint);
                        if (clientPoint.Y <= RESIZE_HANDLE_SIZE)
                        {
                            if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                                m.Result = (IntPtr)13/*HTTOPLEFT*/ ;
                            else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                                m.Result = (IntPtr)12/*HTTOP*/ ;
                            else
                                m.Result = (IntPtr)14/*HTTOPRIGHT*/ ;
                        }
                        else if (clientPoint.Y <= (Size.Height - RESIZE_HANDLE_SIZE))
                        {
                            if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                                m.Result = (IntPtr)10/*HTLEFT*/ ;
                            else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                                m.Result = (IntPtr)2/*HTCAPTION*/ ;
                            else
                                m.Result = (IntPtr)11/*HTRIGHT*/ ;
                        }
                        else
                        {
                            if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                                m.Result = (IntPtr)16/*HTBOTTOMLEFT*/ ;
                            else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                                m.Result = (IntPtr)15/*HTBOTTOM*/ ;
                            else
                                m.Result = (IntPtr)17/*HTBOTTOMRIGHT*/ ;
                        }
                    }
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Rectangle s = User32Stuff.GetScreenBounds();
            Console.WriteLine(string.Format("screen bounds: ({0},{1}) {2}x{3}",
                s.X, s.Y, s.Width, s.Height));
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
