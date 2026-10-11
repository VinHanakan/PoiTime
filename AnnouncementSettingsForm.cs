using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ReportTimePlayer
{
    public sealed class AnnouncementSettingsForm : Form
    {
        private readonly Config original;
        private readonly TrackBar volumeSlider, opacitySlider;
        private readonly CheckBox desktopBox, browserBox, portraitBox, nameBox, lineBox;
        private readonly ComboBox positionBox, backgroundBox, outputBox;
        private readonly TextBox imagePathBox, lineEditor;
        private Color backgroundColor, textColor;
        private string fontFamily;
        private float fontSize;
        private bool fontBold, fontItalic;
        private readonly NumericUpDown hour;
        private bool bindingLine;
        public Dictionary<int, string> EditedLines { get; } = new Dictionary<int, string>();
        public Config Draft => new Config {
            id = original.id, randomVoices = original.randomVoices, volume = volumeSlider.Value,
            showAnnouncement = desktopBox.Checked, browserShowAnnouncement = browserBox.Checked,
            showPortrait = portraitBox.Checked, showName = nameBox.Checked, showLine = lineBox.Checked,
            announcementPosition = new[] { "TopLeft", "TopRight", "Center", "BottomLeft", "BottomRight" }[positionBox.SelectedIndex],
            backgroundMode = backgroundBox.SelectedIndex == 1 ? "Image" : "Color",
            backgroundImage = imagePathBox.Text, backgroundColor = ColorTranslator.ToHtml(backgroundColor), backgroundOpacity = opacitySlider.Value,
            fontFamily = fontFamily, fontSize = fontSize, fontBold = fontBold, fontItalic = fontItalic, textColor = ColorTranslator.ToHtml(textColor),
            audioOutput = new[] { "Both", "Local", "Browser" }[outputBox.SelectedIndex], browserPort = original.browserPort
        };
        public AnnouncementSettingsForm(Config config, Action<int, Config> preview, Action stop)
            : this(config, (h, c, line) => preview(h, c), stop, null) { }
        public AnnouncementSettingsForm(Config config, Action<int, Config, string> preview, Action stop, Func<int, string> getLine)
        {
            original = config; Text = "报时设置"; Font = new Font("微软雅黑", 9);
            ClientSize = new Size(500, 590); FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen; MaximizeBox = false;
            var tabs = new TabControl { Location = new Point(16, 16), Size = new Size(468, 480) };
            var audio = new TabPage("音频输出"); var appearance = new TabPage("立绘与台词"); var dialogue = new TabPage("编辑台词");
            tabs.TabPages.AddRange(new[] { audio, appearance, dialogue }); Controls.Add(tabs);
            LabelAt(audio, "音量（本机和浏览器共用）", 16, 20);
            var volumeValue = LabelAt(audio, "", 365, 20);
            volumeSlider = new TrackBar { Minimum = 0, Maximum = 100, Value = Math.Max(0, Math.Min(100, config.volume)), TickFrequency = 10, Location = new Point(16, 45), Size = new Size(420, 45) };
            volumeValue.Text = volumeSlider.Value + "%"; volumeSlider.ValueChanged += (s, e) => volumeValue.Text = volumeSlider.Value + "%";
            LabelAt(audio, "声音输出", 16, 115);
            outputBox = Combo(audio, 105, 111, 330, new object[] { "本机播放 + OBS 浏览器源（推荐）", "仅本机播放", "仅 OBS 浏览器源" });
            int outputIndex = Array.IndexOf(new[] { "Both", "Local", "Browser" }, config.audioOutput); outputBox.SelectedIndex = outputIndex < 0 ? 0 : outputIndex;
            LabelAt(audio, "浏览器地址", 16, 160);
            var url = new TextBox { ReadOnly = true, Text = "http://127.0.0.1:" + config.browserPort + "/", Location = new Point(105, 156), Size = new Size(330, 26) };
            audio.Controls.AddRange(new Control[] { volumeSlider, url });
            var copy = new Button { Text = "复制地址", Location = new Point(330, 193), Size = new Size(105, 30) };
            copy.Click += (s, e) => Clipboard.SetText(url.Text); audio.Controls.Add(copy);
            LabelAt(audio, "OBS 浏览器源勾选「通过 OBS 控制音频」。\n需要图像时，将浏览器尺寸设置为直播画布尺寸，\n例如 1920 × 1080。页面其余区域为透明。\n双输出时 OBS 监听保持关闭，避免重复发声。", 16, 250);

            desktopBox = Check(appearance, "桌面显示", 16, 16, config.showAnnouncement);
            browserBox = Check(appearance, "浏览器显示", 230, 16, config.browserShowAnnouncement);
            portraitBox = Check(appearance, "显示立绘", 16, 53, config.showPortrait);
            nameBox = Check(appearance, "显示名字", 160, 53, config.showName);
            lineBox = Check(appearance, "显示台词", 305, 53, config.showLine);
            LabelAt(appearance, "显示位置", 16, 95);
            positionBox = Combo(appearance, 105, 91, 330, new object[] { "左上角", "右上角", "屏幕中央", "左下角", "右下角" });
            int position = Array.IndexOf(new[] { "TopLeft", "TopRight", "Center", "BottomLeft", "BottomRight" }, config.announcementPosition); positionBox.SelectedIndex = position < 0 ? 4 : position;
            LabelAt(appearance, "背景类型", 16, 137);
            backgroundBox = Combo(appearance, 105, 133, 170, new object[] { "纯色", "自定义图片" }); backgroundBox.SelectedIndex = config.backgroundMode == "Image" ? 1 : 0;
            backgroundColor = ParseColor(config.backgroundColor, Color.FromArgb(28, 32, 43)); textColor = ParseColor(config.textColor, Color.White);
            var color = new Button { Text = "选择背景颜色", BackColor = backgroundColor, ForeColor = backgroundColor.GetBrightness() < .5 ? Color.White : Color.Black, Location = new Point(290, 131), Size = new Size(145, 30) };
            color.Click += (s, e) => { using (var dialog = new ColorDialog { Color = backgroundColor, FullOpen = true }) if (dialog.ShowDialog(this) == DialogResult.OK) { backgroundColor = dialog.Color; color.BackColor = backgroundColor; color.ForeColor = backgroundColor.GetBrightness() < .5 ? Color.White : Color.Black; } };
            imagePathBox = new TextBox { Text = config.backgroundImage ?? "", Location = new Point(16, 177), Size = new Size(305, 26) };
            var browse = new Button { Text = "选择图片…", Location = new Point(330, 175), Size = new Size(105, 30) };
            browse.Click += (s, e) => { using (var dialog = new OpenFileDialog { Filter = "背景图片|*.png;*.jpg;*.jpeg;*.bmp;*.gif", CheckFileExists = true }) { if (dialog.ShowDialog(this) != DialogResult.OK) return; try { using (var image = Image.FromFile(dialog.FileName)) { } imagePathBox.Text = dialog.FileName; backgroundBox.SelectedIndex = 1; } catch (Exception ex) { MessageBox.Show(this, "无法读取图片：" + ex.Message, Text); } } };
            LabelAt(appearance, "背景不透明度（0% 为全透明）", 16, 220); var opacityValue = LabelAt(appearance, "", 365, 220);
            opacitySlider = new TrackBar { Minimum = 0, Maximum = 100, Value = Math.Max(0, Math.Min(100, config.backgroundOpacity)), TickFrequency = 10, Location = new Point(16, 245), Size = new Size(420, 45) };
            opacityValue.Text = opacitySlider.Value + "%"; opacitySlider.ValueChanged += (s, e) => opacityValue.Text = opacitySlider.Value + "%";
            fontFamily = config.fontFamily ?? "微软雅黑"; fontSize = float.IsNaN(config.fontSize) || float.IsInfinity(config.fontSize) ? 10 : Math.Max(8, Math.Min(32, config.fontSize)); fontBold = config.fontBold; fontItalic = config.fontItalic;
            var fontButton = new Button { Text = "设置字体、字号和文字颜色…", Location = new Point(16, 307), Size = new Size(300, 32) };
            var fontSummary = LabelAt(appearance, "", 16, 352);
            Action summary = () => fontSummary.Text = fontFamily + "  " + fontSize.ToString("0.#") + " 磅" + (fontBold ? "  粗体" : "") + (fontItalic ? "  斜体" : "") + "  " + ColorTranslator.ToHtml(textColor);
            summary(); fontButton.Click += (s, e) => { using (var font = new Font(fontFamily, fontSize, (fontBold ? FontStyle.Bold : FontStyle.Regular) | (fontItalic ? FontStyle.Italic : FontStyle.Regular))) using (var dialog = new FontDialog { Font = font, Color = textColor, ShowColor = true, MinSize = 8, MaxSize = 32, FontMustExist = true, ShowEffects = false }) { if (dialog.ShowDialog(this) != DialogResult.OK) return; fontFamily = dialog.Font.Name; fontSize = dialog.Font.SizeInPoints; fontBold = dialog.Font.Bold; fontItalic = dialog.Font.Italic; textColor = dialog.Color; summary(); } };
            LabelAt(appearance, "桌面与网页使用同一套外观；透明度仅作用于背景。", 16, 398);
            appearance.Controls.AddRange(new Control[] { color, imagePathBox, browse, opacitySlider, fontButton });
            Action update = () => { imagePathBox.Enabled = browse.Enabled = backgroundBox.SelectedIndex == 1; color.Enabled = backgroundBox.SelectedIndex == 0; };
            backgroundBox.SelectedIndexChanged += (s, e) => update(); update();

            LabelAt(dialogue, "编辑下方所选时刻的台词；保存后写入当前语音包。\n留空时显示报时时间，隐藏台词请取消「显示台词」。", 16, 16);
            lineEditor = new TextBox { Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical, Location = new Point(16, 75), Size = new Size(420, 315), Enabled = getLine != null };
            dialogue.Controls.Add(lineEditor);
            LabelAt(this, "试听时刻", 16, 520);
            hour = new NumericUpDown { Minimum = 0, Maximum = 23, Value = DateTime.Now.Hour, Location = new Point(105, 516), Size = new Size(60, 26) };
            Action bind = () => { bindingLine = true; int h = (int)hour.Value; lineEditor.Text = EditedLines.TryGetValue(h, out var text) ? text : getLine?.Invoke(h) ?? ""; bindingLine = false; };
            hour.ValueChanged += (s, e) => bind(); lineEditor.TextChanged += (s, e) => { if (bindingLine || getLine == null) return; int h = (int)hour.Value; if (lineEditor.Text == (getLine(h) ?? "")) EditedLines.Remove(h); else EditedLines[h] = lineEditor.Text; }; bind();
            var play = new Button { Text = "试听 / 预览", Location = new Point(185, 513), Size = new Size(120, 30) };
            play.Click += (s, e) => preview((int)hour.Value, Draft, getLine == null ? null : lineEditor.Text);
            var stopButton = new Button { Text = "停止", Location = new Point(320, 513), Size = new Size(80, 30) }; stopButton.Click += (s, e) => stop();
            var save = new Button { Text = "保存", Location = new Point(315, 551), Size = new Size(75, 28) };
            save.Click += (s, e) => { if ((Draft.showAnnouncement || Draft.browserShowAnnouncement) && Draft.backgroundMode == "Image") { try { using (var image = Image.FromFile(Draft.backgroundImage)) { } } catch (Exception ex) { MessageBox.Show(this, "请选择可读取的背景图片：" + ex.Message, Text); return; } } DialogResult = DialogResult.OK; };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(409, 551), Size = new Size(75, 28) };
            Controls.AddRange(new Control[] { hour, play, stopButton, save, cancel }); AcceptButton = save; CancelButton = cancel;
            FormClosed += (s, e) => stop();
        }
        private static Color ParseColor(string value, Color fallback) { try { return ColorTranslator.FromHtml(value); } catch { return fallback; } }
        private static CheckBox Check(Control parent, string text, int x, int y, bool value) { var box = new CheckBox { Text = text, Checked = value, AutoSize = true, Location = new Point(x, y) }; parent.Controls.Add(box); return box; }
        private static ComboBox Combo(Control parent, int x, int y, int width, object[] items) { var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(x, y), Size = new Size(width, 26) }; combo.Items.AddRange(items); parent.Controls.Add(combo); return combo; }
        private static Label LabelAt(Control parent, string text, int x, int y) { var label = new Label { Text = text, AutoSize = true, Location = new Point(x, y) }; parent.Controls.Add(label); return label; }
    }
}
