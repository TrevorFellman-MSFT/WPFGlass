using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WPFGlass
{
    public partial class BackgroundMagnifierForm : Form
    {
        NativeWindow _magnifier = new NativeWindow();
        IntPtr bitmapHandle = IntPtr.Zero;

        public BackgroundMagnifierForm()
        {
            if (!MagInitialize())
            {
                throw new Win32Exception("MagInitialize failed");
            }
            this.FormBorderStyle = FormBorderStyle.None;
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (bitmapHandle != IntPtr.Zero)
            {
                DeleteObject(bitmapHandle);
                bitmapHandle = IntPtr.Zero;
            }

            MagUninitialize();
        }

        protected override void OnLoad(EventArgs e)
        {
            bool cloak = true;
            int error = DwmSetWindowAttribute(Handle, DWMWINDOWATTRIBUTE.DWMWA_CLOAK, ref cloak, (uint)Marshal.SizeOf<bool>());
            if (error < 0)
            {
                Marshal.ThrowExceptionForHR(error);
            }
            
            base.OnLoad(e);

            CreateParams cp = new CreateParams();
            cp.ClassName = "Magnifier";
            cp.Style = WS_CHILD | WS_VISIBLE;
            cp.Parent = Handle;
            cp.X = 0;
            cp.Y = 0;
            cp.Width = ClientRectangle.Width;
            cp.Height = ClientRectangle.Height;
            _magnifier.CreateHandle(cp);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (!MoveWindow(_magnifier.Handle, 0, 0, ClientSize.Width, ClientSize.Height, false))
            {
                new Win32Exception(Marshal.GetLastWin32Error(), "MoveWindow failed.");
            }
        }

        internal void ExcludeWindow(IntPtr window)
        {
            IntPtr[] windows = new IntPtr[1];
            windows[0] = window;
            if (!MagSetWindowFilterList(_magnifier.Handle, MW_FILTERMODE_EXCLUDE, 1, windows))
            {
                new Win32Exception("MagSetWindowFilterList failed.");
            }
        }

        internal void UpdateBackground(Rectangle desktop)
        {
            RECT sourceRect = ToRECT(desktop);

            if (!MagSetWindowSource(_magnifier.Handle, sourceRect))
            {
                new Win32Exception("MagSetWindowSource failed.");
            }
        }


        public IntPtr GetHBitmap(IntPtr hWnd, int x, int y, int width, int height)
        {

            Rectangle rc = new Rectangle(x, y, width, height);
            ExcludeWindow(hWnd);
            Size = rc.Size;
            UpdateBackground(rc);

            if (bitmapHandle != IntPtr.Zero)
            {
                DeleteObject(bitmapHandle);
                bitmapHandle = IntPtr.Zero;
            }

            if (ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                using (Bitmap bitmap = new Bitmap(ClientSize.Width, ClientSize.Height))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        IntPtr dc = graphics.GetHdc();

                        if (dc != IntPtr.Zero)
                        {
                            try
                            {
                                PrintWindow(Handle, dc, PW_RENDERFULLCONTENT);
                            }
                            finally
                            {
                                graphics.ReleaseHdc(dc);
                            }
                        }
                    }

                    bitmapHandle = bitmap.GetHbitmap();
                }
            }

            return bitmapHandle;
        }

        Rectangle ToRectangle(RECT rc)
        {
            return new Rectangle(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
        }

        RECT ToRECT(Rectangle rc)
        {
            return new RECT() { left = rc.X, top = rc.Y, right = rc.Right, bottom = rc.Bottom};
        }

        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int LWA_ALPHA = 0x00000002;

        private const uint MW_FILTERMODE_EXCLUDE = 0;
        enum DWMWINDOWATTRIBUTE
        {
            DWMWA_NCRENDERING_ENABLED = 1,      // [get] Is non-client rendering enabled/disabled
            DWMWA_NCRENDERING_POLICY,           // [set] DWMNCRENDERINGPOLICY - Non-client rendering policy
            DWMWA_TRANSITIONS_FORCEDISABLED,    // [set] Potentially enable/forcibly disable transitions
            DWMWA_ALLOW_NCPAINT,                // [set] Allow contents rendered in the non-client area to be visible on the DWM-drawn frame.
            DWMWA_CAPTION_BUTTON_BOUNDS,        // [get] Bounds of the caption button area in window-relative space.
            DWMWA_NONCLIENT_RTL_LAYOUT,         // [set] Is non-client content RTL mirrored
            DWMWA_FORCE_ICONIC_REPRESENTATION,  // [set] Force this window to display iconic thumbnails.
            DWMWA_FLIP3D_POLICY,                // [set] Designates how Flip3D will treat the window.
            DWMWA_EXTENDED_FRAME_BOUNDS,        // [get] Gets the extended frame bounds rectangle in screen space
            DWMWA_HAS_ICONIC_BITMAP,            // [set] Indicates an available bitmap when there is no better thumbnail representation.
            DWMWA_DISALLOW_PEEK,                // [set] Don't invoke Peek on the window.
            DWMWA_EXCLUDED_FROM_PEEK,           // [set] LivePreview exclusion information
            DWMWA_CLOAK,                        // [set] Cloak or uncloak the window
            DWMWA_CLOAKED,                      // [get] Gets the cloaked state of the window
            DWMWA_FREEZE_REPRESENTATION,        // [set] BOOL, Force this window to freeze the thumbnail without live update
            DWMWA_LAST
        };

        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("magnification.dll", ExactSpelling = true)]
        private static extern bool MagSetWindowSource(IntPtr hwnd, RECT rect);

        [DllImport("magnification.dll", ExactSpelling = true)]
        private static extern bool MagSetWindowFilterList(IntPtr hwnd, uint dwFilterMode, int count, IntPtr[] pHWND);

        [DllImport("dwmapi.dll", ExactSpelling = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, [In] ref bool pvAttribute, uint cbAttribute);

        private const int PW_RENDERFULLCONTENT = 0x00000002;

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern bool DeleteObject(IntPtr ho);

        [DllImport("Magnification.dll", ExactSpelling = true)]
        private static extern bool MagInitialize();

        [DllImport("Magnification.dll", ExactSpelling = true)]
        private static extern bool MagUninitialize();
    }
}
