using System;
using System.Drawing;
using System.Windows.Forms;

namespace ReportTimePlayer
{
    public sealed class ObsCaptureForm : Form
    {
        protected override bool ShowWithoutActivation => true;
        public ObsCaptureForm(Action preview, Action stop, Action settings)
        {
            Text = "PoiTime — OBS 音频";
            Font = new Font("微软雅黑", 9);
            ClientSize = new Size(430, 215);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            var help = new Label { Location = new Point(16, 16), Size = new Size(398, 120), Text = "在 OBS 的「应用程序音频采集」中选择：\n[PoiTime.exe] PoiTime — OBS 音频\n\n匹配优先级选择按可执行程序匹配。\n此窗口可最小化，关闭按钮也会最小化。\n若出现回声，请避免同时采集同一份桌面音频。" };
            var play = new Button { Text = "试听当前时刻", Location = new Point(16, 153), Size = new Size(125, 32) };
            var stopButton = new Button { Text = "停止试听", Location = new Point(153, 153), Size = new Size(120, 32) };
            var options = new Button { Text = "报时设置", Location = new Point(285, 153), Size = new Size(125, 32) };
            play.Click += (s, e) => preview(); stopButton.Click += (s, e) => stop(); options.Click += (s, e) => settings();
            Controls.AddRange(new Control[] { help, play, stopButton, options });
            WindowState = FormWindowState.Minimized;
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; WindowState = FormWindowState.Minimized; }
            base.OnFormClosing(e);
        }
        public void Restore() { WindowState = FormWindowState.Normal; Show(); Activate(); }
    }
}
