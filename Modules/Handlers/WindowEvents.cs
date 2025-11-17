using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace NMH_Media_Player.Modules.Handlers
{
    public static class WindowEvents
    {
        private const int edge = 5; // thickness for resize detection

        // PInvoke for resizing
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        // ====== Drag / Resize Logic ======
        public static void TitleBar_MouseDown(Window window, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Point pos = e.GetPosition(window);

            // Detect edges/corners
            IntPtr resizeDir = IntPtr.Zero;
            if (pos.X <= edge && pos.Y <= edge) resizeDir = (IntPtr)HTTOPLEFT;
            else if (pos.X >= window.ActualWidth - edge && pos.Y <= edge) resizeDir = (IntPtr)HTTOPRIGHT;
            else if (pos.X <= edge && pos.Y >= window.ActualHeight - edge) resizeDir = (IntPtr)HTBOTTOMLEFT;
            else if (pos.X >= window.ActualWidth - edge && pos.Y >= window.ActualHeight - edge) resizeDir = (IntPtr)HTBOTTOMRIGHT;
            else if (pos.X <= edge) resizeDir = (IntPtr)HTLEFT;
            else if (pos.X >= window.ActualWidth - edge) resizeDir = (IntPtr)HTRIGHT;
            else if (pos.Y <= edge) resizeDir = (IntPtr)HTTOP;
            else if (pos.Y >= window.ActualHeight - edge) resizeDir = (IntPtr)HTBOTTOM;

            if (resizeDir != IntPtr.Zero)
            {
                // Resize window
                ReleaseCapture();
                SendMessage(new System.Windows.Interop.WindowInteropHelper(window).Handle,
                            WM_NCLBUTTONDOWN, resizeDir, IntPtr.Zero);
            }
            else
            {
                // Drag window normally
                window.DragMove();
            }
        }

        // ===== Window Control Buttons =====
        public static void BtnMinimize_Click(Window window)
        {
            window.WindowState = WindowState.Minimized;
        }

        public static void BtnMaximize_Click(Window window)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        public static void BtnClose_Click(Window window, MediaController mediaController)
        {
            try
            {
                if (mediaController?._analysisCts != null)
                {
                    mediaController._analysisCts.Cancel();
                    mediaController._analysisCts.Dispose();
                    mediaController._analysisCts = null;
                }

                window?.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during window close: {ex}");
            }
        }

        public static void CloseButton_MouseEnter() { }
        public static void CloseButton_MouseLeave() { }
    }
}
