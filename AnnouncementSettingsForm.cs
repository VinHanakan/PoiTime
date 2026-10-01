using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ReportTimePlayer
{
    public sealed class AnnouncementSettingsForm : Form
    {
        private readonly TrackBar volumeSlider, opacitySlider;
        private readonly CheckBox showAnnouncementBox;
        private readonly ComboBox positionBox, backgroundBox;
        private readonly TextBox imagePathBox;
        private readonly Button colorButton;
        private Color backgroundColor;
        private readonly Config original;
        public Config Draft => new Config {
            id = original.id, randomVoices = original.randomVoices,
            volume = volumeSlider.Value, showAnnouncement = showAnnouncementBox.Checked,
            announcementPosition = new[] { "TopLeft", "TopRight", "Center", "BottomLeft", "BottomRight" }[positionBox.SelectedIndex],
            backgroundMode = backgroundBox.SelectedIndex == 1 ? "Image" : "Color",
            backgroundImage = imagePathBox.Text, backgroundColor = ColorTranslator.ToHtml(backgroundColor),
            backgroundOpacity = opacitySlider.Value
        };

        public AnnouncementSettingsForm(Config config, Action<int, Config> preview, Action stop)
        {
            original = config;
            Text = "报时设置";
            Font = new Font("微软雅黑", 9);
            ClientSize = new Size(440, 440);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            LabelAt("音量", 16, 18);
            var volumeValue = LabelAt("", 365, 18);
            volumeSlider = new TrackBar { Minimum = 0, Maximum = 100, Value = Math.Max(0, Math.Min(100, config.volume)), TickFrequency = 10, Location = new Point(16, 43), Size = new Size(408, 45) };
            volumeValue.Text = volumeSlider.Value + "%";
            volumeSlider.ValueChanged += (s, e) => volumeValue.Text = volumeSlider.Value + "%";
            showAnnouncementBox = new CheckBox { Text = "报时显示立绘与台词", Checked = config.showAnnouncement, AutoSize = true, Location = new Point(16, 93) };
            LabelAt("弹窗位置", 16, 132);
            positionBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(105, 128), Size = new Size(310, 26) };
            positionBox.Items.AddRange(new object[] { "左上角", "右上角", "屏幕中央", "左下角", "右下角" });
            int index = Array.IndexOf(new[] { "TopLeft", "TopRight", "Center", "BottomLeft", "BottomRight" }, config.announcementPosition);
            positionBox.SelectedIndex = index < 0 ? 4 : index;
            LabelAt("背景类型", 16, 172);
            backgroundBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(105, 168), Size = new Size(155, 26) };
            backgroundBox.Items.AddRange(new object[] { "纯色", "自定义图片" });
            backgroundBox.SelectedIndex = config.backgroundMode == "Image" ? 1 : 0;
            try { backgroundColor = ColorTranslator.FromHtml(config.backgroundColor); } catch { backgroundColor = Color.FromArgb(28, 32, 43); }
            colorButton = new Button { Text = "选择颜色", BackColor = backgroundColor, ForeColor = backgroundColor.GetBrightness() < .5 ? Color.White : Color.Black, Location = new Point(280, 166), Size = new Size(135, 28) };
            colorButton.Click += (s, e) => { using (var dialog = new ColorDialog { Color = backgroundColor, FullOpen = true }) { if (dialog.ShowDialog(this) == DialogResult.OK) { backgroundColor = dialog.Color; colorButton.BackColor = backgroundColor; colorButton.ForeColor = backgroundColor.GetBrightness() < .5 ? Color.White : Color.Black; } } };
            imagePathBox = new TextBox { Text = config.backgroundImage ?? "", Location = new Point(16, 210), Size = new Size(305, 26) };
            var browse = new Button { Text = "选择图片…", Location = new Point(330, 208), Size = new Size(85, 28) };
            browse.Click += (s, e) => {
                using (var dialog = new OpenFileDialog { Filter = "背景图片|*.png;*.jpg;*.jpeg;*.bmp;*.gif", CheckFileExists = true }) {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    try { using (var image = Image.FromFile(dialog.FileName)) { } imagePathBox.Text = dialog.FileName; backgroundBox.SelectedIndex = 1; }
                    catch (Exception ex) { MessageBox.Show(this, "无法读取图片：" + ex.Message, Text); }
                }
            };
            LabelAt("背景不透明度（0% 为全透明）", 16, 252);
            var opacityValue = LabelAt("", 365, 252);
            opacitySlider = new TrackBar { Minimum = 0, Maximum = 100, Value = Math.Max(0, Math.Min(100, config.backgroundOpacity)), TickFrequency = 10, Location = new Point(16, 277), Size = new Size(408, 45) };
            opacityValue.Text = opacitySlider.Value + "%";
            opacitySlider.ValueChanged += (s, e) => opacityValue.Text = opacitySlider.Value + "%";
            Action update = () => { bool enabled = showAnnouncementBox.Checked; positionBox.Enabled = backgroundBox.Enabled = opacitySlider.Enabled = enabled; colorButton.Enabled = enabled && backgroundBox.SelectedIndex == 0; imagePathBox.Enabled = browse.Enabled = enabled && backgroundBox.SelectedIndex == 1; };
            showAnnouncementBox.CheckedChanged += (s, e) => update();
            backgroundBox.SelectedIndexChanged += (s, e) => update();
            update();
            LabelAt("试听时刻", 16, 337);
            var hour = new NumericUpDown { Minimum = 0, Maximum = 23, Value = DateTime.Now.Hour, Location = new Point(105, 333), Size = new Size(60, 26) };
            var play = new Button { Text = "试听", Location = new Point(185, 331), Size = new Size(105, 30) };
            play.Click += (s, e) => preview((int)hour.Value, Draft);
            var stopButton = new Button { Text = "停止", Location = new Point(310, 331), Size = new Size(105, 30) };
            stopButton.Click += (s, e) => stop();
            LabelAt("试听使用当前设置；点击保存后用于整点报时。", 16, 371);
            var save = new Button { Text = "保存", Location = new Point(245, 402), Size = new Size(75, 28) };
            save.Click += (s, e) => {
                if (Draft.showAnnouncement && Draft.backgroundMode == "Image") {
                    try { using (var image = Image.FromFile(Draft.backgroundImage)) { } }
                    catch (Exception ex) { MessageBox.Show(this, "请选择可读取的背景图片：" + ex.Message, Text); return; }
                }
                DialogResult = DialogResult.OK;
            };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(340, 402), Size = new Size(75, 28) };
            Controls.AddRange(new Control[] { volumeSlider, showAnnouncementBox, positionBox, backgroundBox, colorButton, imagePathBox, browse, opacitySlider, hour, play, stopButton, save, cancel });
            AcceptButton = save; CancelButton = cancel;
            FormClosed += (s, e) => stop();
        }
        private Label LabelAt(string text, int x, int y) { var label = new Label { Text = text, AutoSize = true, Location = new Point(x, y) }; Controls.Add(label); return label; }
    }
}
