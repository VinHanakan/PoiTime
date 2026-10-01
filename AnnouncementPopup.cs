using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ReportTimePlayer
{
    public sealed class AnnouncementPopup : Form
    {
        private readonly Timer closeTimer;
        private readonly Bitmap surface;
        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams { get { var p = base.CreateParams; p.ExStyle |= 0x08000000 | 0x00000080 | 0x00080000; return p; } }
        public AnnouncementPopup(string name, string line, string portraitPath, Config settings, TimeSpan duration)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false; TopMost = true;
            ClientSize = new Size(360, 250); StartPosition = FormStartPosition.Manual;
            surface = RenderSurface(name, line, portraitPath, settings);
            var area = Screen.PrimaryScreen.WorkingArea;
            const int margin = 16;
            int x = area.Right - Width - margin, y = area.Bottom - Height - margin;
            switch (settings.announcementPosition) {
                case "TopLeft": x = area.Left + margin; y = area.Top + margin; break;
                case "TopRight": y = area.Top + margin; break;
                case "Center": x = area.Left + (area.Width - Width) / 2; y = area.Top + (area.Height - Height) / 2; break;
                case "BottomLeft": x = area.Left + margin; break;
            }
            Location = new Point(x, y);
            closeTimer = new Timer { Interval = (int)Math.Max(3000, Math.Min(60000, duration.TotalMilliseconds + 500)) };
            closeTimer.Tick += (s, e) => Close();
        }
        // Per-pixel alpha keeps the portrait and text opaque while only the background fades.
        public static Bitmap RenderSurface(string name, string line, string portraitPath, Config settings)
        {
            var bitmap = new Bitmap(360, 250, PixelFormat.Format32bppPArgb);
            try {
                using (var g = Graphics.FromImage(bitmap)) {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    float alpha = Math.Max(0, Math.Min(100, settings.backgroundOpacity)) / 100f;
                    Color color;
                    try { color = ColorTranslator.FromHtml(settings.backgroundColor); } catch { color = Color.FromArgb(28, 32, 43); }
                    using (var brush = new SolidBrush(Color.FromArgb((int)(255 * alpha), color))) g.FillRectangle(brush, 0, 0, 360, 250);
                    if (settings.backgroundMode == "Image" && File.Exists(settings.backgroundImage)) {
                        try {
                            using (var image = Image.FromFile(settings.backgroundImage))
                            using (var attributes = new ImageAttributes()) {
                                var matrix = new ColorMatrix { Matrix33 = alpha };
                                attributes.SetColorMatrix(matrix);
                                attributes.SetWrapMode(WrapMode.TileFlipXY);
                                float scale = Math.Max(360f / image.Width, 250f / image.Height);
                                float w = 360 / scale, h = 250 / scale;
                                g.CompositingMode = CompositingMode.SourceCopy;
                                g.DrawImage(image, new Rectangle(0, 0, 360, 250), (image.Width - w) / 2, (image.Height - h) / 2, w, h, GraphicsUnit.Pixel, attributes);
                                g.CompositingMode = CompositingMode.SourceOver;
                            }
                        } catch { g.CompositingMode = CompositingMode.SourceOver; }
                    }
                    bool portrait = false;
                    if (File.Exists(portraitPath)) {
                        try { using (var image = Image.FromFile(portraitPath)) { float scale = Math.Min(120f / image.Width, 234f / image.Height); float w = image.Width * scale, h = image.Height * scale; g.DrawImage(image, 8 + (120 - w) / 2, 8 + (234 - h) / 2, w, h); portrait = true; } } catch { }
                    }
                    int left = portrait ? 137 : 18, width = portrait ? 210 : 325;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    using (var titleFont = new Font("微软雅黑", 11, FontStyle.Bold))
                    using (var lineFont = new Font("微软雅黑", 10))
                    using (var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter }) {
                        g.DrawString(name ?? "", titleFont, Brushes.White, new RectangleF(left, 35, width, 32), format);
                        g.DrawString(line ?? "", lineFont, Brushes.White, new RectangleF(left, 75, width, 145), format);
                    }
                }
                return bitmap;
            } catch { bitmap.Dispose(); throw; }
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            IntPtr screen = GetDC(IntPtr.Zero), memory = IntPtr.Zero, handle = IntPtr.Zero, previous = IntPtr.Zero;
            try {
                memory = CreateCompatibleDC(screen);
                handle = surface.GetHbitmap(Color.FromArgb(0));
                previous = SelectObject(memory, handle);
                var size = new NativeSize { Width = Width, Height = Height };
                var target = new NativePoint { X = Left, Y = Top }; var source = new NativePoint();
                var blend = new Blend { SourceConstantAlpha = 255, AlphaFormat = 1 };
                if (!UpdateLayeredWindow(Handle, screen, ref target, ref size, memory, ref source, 0, ref blend, 2))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                closeTimer.Start();
            } finally { if (previous != IntPtr.Zero) SelectObject(memory, previous); if (handle != IntPtr.Zero) DeleteObject(handle); if (memory != IntPtr.Zero) DeleteDC(memory); if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen); }
        }
        protected override void Dispose(bool disposing) { if (disposing) { closeTimer?.Dispose(); surface?.Dispose(); } base.Dispose(disposing); }
        [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] private struct NativeSize { public int Width, Height; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] private struct Blend { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr dc, ref NativePoint target, ref NativeSize size, IntPtr sourceDC, ref NativePoint source, uint key, ref Blend blend, uint flags);
    }
}
