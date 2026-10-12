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
            ClientSize = surface.Size;
            var area = Screen.PrimaryScreen.WorkingArea;
            const int margin = 16;
            int x = area.Right - Width - margin, y = area.Bottom - Height - margin;
            switch (settings.announcementPosition) {
                case "TopLeft": x = area.Left + margin; y = area.Top + margin; break;
                case "TopRight": y = area.Top + margin; break;
                case "Center": x = area.Left + (area.Width - Width) / 2; y = area.Top + (area.Height - Height) / 2; break;
                case "BottomLeft": x = area.Left + margin; break;
            }
            Location = new Point(x + settings.positionOffsetX, y + settings.positionOffsetY);
            closeTimer = new Timer { Interval = (int)Math.Max(3000, Math.Min(60000, duration.TotalMilliseconds + 500)) };
            closeTimer.Tick += (s, e) => Close();
        }
        public static Size MeasureRegion(string name, string line, string portraitPath, Config settings)
        {
            var size = BaseRegion(name, line, portraitPath, settings);
            int percent = Math.Max(25, Math.Min(300, settings.regionScale));
            return new Size((int)Math.Round(size.Width * percent / 100.0), (int)Math.Round(size.Height * percent / 100.0));
        }
        private static Size BaseRegion(string name, string line, string portraitPath, Config settings)
        {
            int width = Math.Max(240, Math.Min(1200, settings.regionWidth));
            if (settings.regionHeight > 0) return new Size(width, Math.Max(100, Math.Min(1200, settings.regionHeight)));
            bool portrait = settings.showPortrait && File.Exists(portraitPath);
            float fontSize = float.IsNaN(settings.fontSize) || float.IsInfinity(settings.fontSize) ? 10 : Math.Max(8, Math.Min(32, settings.fontSize));
            int left = portrait ? 137 : 18;
            float bottom = portrait ? 242 : 0;
            using (var title = new Font(settings.fontFamily ?? "微软雅黑", fontSize + 1, (settings.fontBold ? FontStyle.Bold : FontStyle.Regular) | (settings.fontItalic ? FontStyle.Italic : FontStyle.Regular)))
            using (var font = new Font(settings.fontFamily ?? "微软雅黑", fontSize, title.Style))
            using (var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter }) {
                if (settings.showName) bottom = Math.Max(bottom, TextBottom(name, title, new RectangleF(left, 25, width - left - 13, Math.Max(32, fontSize * 2)), format));
                float top = settings.showName ? 30 + Math.Max(32, fontSize * 2) : 25;
                if (settings.showLine) bottom = Math.Max(bottom, TextBottom(line, font, new RectangleF(left, top, width - left - 13, 10000), format));
            }
            float stroke = settings.textOutline && !float.IsNaN(settings.textOutlineWidth) && !float.IsInfinity(settings.textOutlineWidth) ? Math.Max(.5f, Math.Min(8, settings.textOutlineWidth)) / 2 : 0;
            return new Size(width, Math.Max(100, Math.Min(1200, (int)Math.Ceiling(bottom + stroke + 20))));
        }
        private static float TextBottom(string text, Font font, RectangleF bounds, StringFormat format)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            using (var path = new GraphicsPath()) { path.AddString(text, font.FontFamily, (int)font.Style, font.SizeInPoints * 96f / 72f, bounds, format); return path.GetBounds().Bottom; }
        }
        // Per-pixel alpha keeps the portrait and text opaque while only the background fades.
        public static Bitmap RenderSurface(string name, string line, string portraitPath, Config settings)
        {
            var region = BaseRegion(name, line, portraitPath, settings);
            int width = region.Width, height = region.Height;
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            try {
                using (var g = Graphics.FromImage(bitmap)) {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    float alpha = Math.Max(0, Math.Min(100, settings.backgroundOpacity)) / 100f;
                    Color color;
                    try { color = ColorTranslator.FromHtml(settings.backgroundColor); } catch { color = Color.FromArgb(28, 32, 43); }
                    using (var brush = new SolidBrush(Color.FromArgb((int)(255 * alpha), color))) g.FillRectangle(brush, 0, 0, width, height);
                    if (settings.backgroundMode == "Image" && File.Exists(settings.backgroundImage)) {
                        try {
                            using (var image = Image.FromFile(settings.backgroundImage))
                            using (var attributes = new ImageAttributes()) {
                                var matrix = new ColorMatrix { Matrix33 = alpha };
                                attributes.SetColorMatrix(matrix);
                                attributes.SetWrapMode(WrapMode.TileFlipXY);
                                float scale = Math.Max((float)width / image.Width, (float)height / image.Height);
                                float w = width / scale, h = height / scale;
                                g.CompositingMode = CompositingMode.SourceCopy;
                                g.DrawImage(image, new Rectangle(0, 0, width, height), (image.Width - w) / 2, (image.Height - h) / 2, w, h, GraphicsUnit.Pixel, attributes);
                                g.CompositingMode = CompositingMode.SourceOver;
                            }
                        } catch { g.CompositingMode = CompositingMode.SourceOver; }
                    }
                    bool portrait = false;
                    if (settings.showPortrait && File.Exists(portraitPath)) {
                        try { using (var image = Image.FromFile(portraitPath)) { float scale = Math.Min(120f / image.Width, 234f / image.Height); float w = image.Width * scale, h = image.Height * scale; g.DrawImage(image, 8 + (120 - w) / 2, 8 + (234 - h) / 2, w, h); portrait = true; } } catch { }
                    }
                    int left = portrait ? 137 : 18, textWidth = width - left - 13;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    float size = float.IsNaN(settings.fontSize) || float.IsInfinity(settings.fontSize) ? 10 : Math.Max(8, Math.Min(32, settings.fontSize));
                    Color textColor;
                    try { textColor = ColorTranslator.FromHtml(settings.textColor); } catch { textColor = Color.White; }
                    using (var brush = new SolidBrush(textColor))
                    using (var titleFont = new Font(settings.fontFamily ?? "微软雅黑", size + 1, (settings.fontBold ? FontStyle.Bold : FontStyle.Regular) | (settings.fontItalic ? FontStyle.Italic : FontStyle.Regular)))
                    using (var lineFont = new Font(settings.fontFamily ?? "微软雅黑", size, (settings.fontBold ? FontStyle.Bold : FontStyle.Regular) | (settings.fontItalic ? FontStyle.Italic : FontStyle.Regular)))
                    using (var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter }) {
                        if (settings.showName) DrawText(g, name ?? "", titleFont, brush, new RectangleF(left, 25, textWidth, Math.Max(32, size * 2)), format, settings);
                        float top = settings.showName ? 30 + Math.Max(32, size * 2) : 25;
                        if (settings.showLine) DrawText(g, line ?? "", lineFont, brush, new RectangleF(left, top, textWidth, height - 20 - top), format, settings);
                    }
                }
                int percent = Math.Max(25, Math.Min(300, settings.regionScale));
                if (percent == 100) return bitmap;
                var scaled = new Bitmap((int)Math.Round(width * percent / 100.0), (int)Math.Round(height * percent / 100.0), PixelFormat.Format32bppPArgb);
                try { using (var graphics = Graphics.FromImage(scaled)) { graphics.InterpolationMode = InterpolationMode.HighQualityBicubic; using (var attributes = new ImageAttributes()) { attributes.SetWrapMode(WrapMode.TileFlipXY); graphics.DrawImage(bitmap, new Rectangle(0, 0, scaled.Width, scaled.Height), 0, 0, width, height, GraphicsUnit.Pixel, attributes); } } }
                catch { scaled.Dispose(); throw; }
                bitmap.Dispose(); return scaled;
            } catch { bitmap.Dispose(); throw; }
        }
        private static void DrawText(Graphics graphics, string text, Font font, Brush fill, RectangleF bounds, StringFormat format, Config settings)
        {
            if (!settings.textOutline) { graphics.DrawString(text, font, fill, bounds, format); return; }
            Color color;
            try { color = ColorTranslator.FromHtml(settings.textOutlineColor); } catch { color = Color.Black; }
            float width = float.IsNaN(settings.textOutlineWidth) || float.IsInfinity(settings.textOutlineWidth) ? 2 : Math.Max(.5f, Math.Min(8, settings.textOutlineWidth));
            using (var path = new GraphicsPath())
            using (var pen = new Pen(color, width) { LineJoin = LineJoin.Round }) {
                path.AddString(text, font.FontFamily, (int)font.Style, font.SizeInPoints * graphics.DpiY / 72f, bounds, format);
                var state = graphics.Save();
                try {
                    graphics.SetClip(RectangleF.Inflate(bounds, width, width), CombineMode.Intersect);
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.DrawPath(pen, path); graphics.FillPath(fill, path);
                } finally { graphics.Restore(state); }
            }
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
