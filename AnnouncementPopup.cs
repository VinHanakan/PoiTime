using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ReportTimePlayer
{
    public sealed class AnnouncementPopup : Form
    {
        private readonly Timer closeTimer;
        private readonly PictureBox portraitBox;

        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ExStyle |= 0x08000000 | 0x00000080; // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
                return parameters;
            }
        }

        public AnnouncementPopup(string name, string line, string portraitPath,
            string position, TimeSpan duration)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(28, 32, 43);
            ClientSize = new Size(360, 250);
            StartPosition = FormStartPosition.Manual;

            portraitBox = new PictureBox
            {
                Location = new Point(8, 8), Size = new Size(120, 234),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            bool hasPortrait = File.Exists(portraitPath);
            if (hasPortrait)
            {
                try
                {
                    using (var source = Image.FromFile(portraitPath))
                        portraitBox.Image = new Bitmap(source);
                }
                catch (Exception) { hasPortrait = false; }
            }
            if (!hasPortrait) portraitBox.Visible = false;

            int textLeft = hasPortrait ? 137 : 18;
            int textWidth = hasPortrait ? 210 : 325;

            var nameLabel = new Label
            {
                Text = name, ForeColor = Color.White,
                Font = new Font("微软雅黑", 11, FontStyle.Bold),
                Location = new Point(textLeft, 35), Size = new Size(textWidth, 32)
            };
            var lineLabel = new Label
            {
                Text = line, ForeColor = Color.White,
                Font = new Font("微软雅黑", 10),
                Location = new Point(textLeft, 75), Size = new Size(textWidth, 145),
                AutoEllipsis = true
            };
            Controls.AddRange(new Control[] { portraitBox, nameLabel, lineLabel });

            var area = Screen.PrimaryScreen.WorkingArea;
            const int margin = 16;
            int x = area.Right - Width - margin;
            int y = area.Bottom - Height - margin;
            switch (position)
            {
                case "TopLeft": x = area.Left + margin; y = area.Top + margin; break;
                case "TopRight": y = area.Top + margin; break;
                case "Center": x = area.Left + (area.Width - Width) / 2;
                    y = area.Top + (area.Height - Height) / 2; break;
                case "BottomLeft": x = area.Left + margin; break;
            }
            Location = new Point(x, y);

            closeTimer = new Timer
            {
                Interval = (int)Math.Max(3000, Math.Min(60000, duration.TotalMilliseconds + 500))
            };
            closeTimer.Tick += (sender, args) => Close();
            closeTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                closeTimer?.Dispose();
                portraitBox?.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
