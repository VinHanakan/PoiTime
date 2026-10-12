using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ReportTimePlayer
{
    public sealed class AnnouncementSettingsForm : Form
    {
        private readonly Config original;
        private readonly TrackBar volumeSlider, opacitySlider;
        private readonly CheckBox desktopBox, browserBox, portraitBox, nameBox, lineBox;
        private readonly ComboBox positionBox, backgroundBox, outputBox, languageBox;
        private readonly TextBox imagePathBox, lineEditor, japaneseEditor;
        private Color backgroundColor, textColor, outlineColor;
        private readonly CheckBox outlineBox;
        private readonly NumericUpDown outlineWidth;
        private string fontFamily;
        private float fontSize;
        private bool fontBold, fontItalic;
        private readonly NumericUpDown hour;
        private bool bindingLine;
        private readonly ComboBox packBox;
        private readonly CheckBox randomBox;
        private readonly NumericUpDown regionWidthBox, regionHeightBox, scaleBox, offsetXBox, offsetYBox;
        private readonly ReportTimeContext context;
        private readonly Dictionary<string, Dictionary<int, string>> chineseEdits = new Dictionary<string, Dictionary<int, string>>();
        private readonly Dictionary<string, Dictionary<int, string>> japaneseEdits = new Dictionary<string, Dictionary<int, string>>();
        private readonly Dictionary<string, VoiceFolderConfig> packCache = new Dictionary<string, VoiceFolderConfig>();
        private readonly List<string> packIds = new List<string>();
        private bool busy;
        private string SelectedPack => context == null ? original.id ?? "" : (packBox == null || packBox.SelectedIndex < 0) ? context.CurrentVoicePackId : packIds[packBox.SelectedIndex];
        public IEnumerable<string> EditedPackIds => chineseEdits.Keys.Union(japaneseEdits.Keys).ToArray().Where(id => LinesForPack(id, false).Count > 0 || LinesForPack(id, true).Count > 0);
        public Dictionary<int, string> LinesForPack(string id, bool japanese) { var edits = japanese ? japaneseEdits : chineseEdits; if (!edits.TryGetValue(id, out var lines)) edits[id] = lines = new Dictionary<int, string>(); return lines; }
        public Dictionary<int, string> EditedLines => LinesForPack(SelectedPack, false);
        public Dictionary<int, string> EditedJapaneseLines => LinesForPack(SelectedPack, true);
        private string PackLine(int h, bool japanese) { if (!packCache.TryGetValue(SelectedPack, out var pack)) packCache[SelectedPack] = pack = context.ReadVoicePack(SelectedPack); var voice = pack.voices?.FirstOrDefault(v => v.hour == h); return japanese ? voice?.japaneseText : voice?.text; }
        public Config Draft => new Config {
            id = SelectedPack, randomVoices = randomBox.Checked, volume = volumeSlider.Value,
            showAnnouncement = desktopBox.Checked, browserShowAnnouncement = browserBox.Checked,
            showPortrait = portraitBox.Checked, showName = nameBox.Checked, showLine = lineBox.Checked, dialogueLanguage = new[] { "Chinese", "Japanese", "Bilingual" }[languageBox.SelectedIndex],
            announcementPosition = new[] { "TopLeft", "TopRight", "Center", "BottomLeft", "BottomRight" }[positionBox.SelectedIndex],
            regionWidth = (int)regionWidthBox.Value, regionHeight = (int)regionHeightBox.Value, regionScale = (int)scaleBox.Value, positionOffsetX = (int)offsetXBox.Value, positionOffsetY = (int)offsetYBox.Value,
            backgroundMode = backgroundBox.SelectedIndex == 1 ? "Image" : "Color",
            backgroundImage = imagePathBox.Text, backgroundColor = ColorTranslator.ToHtml(backgroundColor), backgroundOpacity = opacitySlider.Value,
            fontFamily = fontFamily, fontSize = fontSize, fontBold = fontBold, fontItalic = fontItalic, textColor = ColorTranslator.ToHtml(textColor),
            textOutline = outlineBox.Checked, textOutlineColor = ColorTranslator.ToHtml(outlineColor), textOutlineWidth = (float)outlineWidth.Value,
            audioOutput = new[] { "Both", "Local", "Browser" }[outputBox.SelectedIndex], browserPort = original.browserPort
        };
        public AnnouncementSettingsForm(Config config, Action<int, Config> preview, Action stop)
            : this(config, (h, c, line) => preview(h, c), stop, null) { }
        public AnnouncementSettingsForm(Config config, Action<int, Config, string> preview, Action stop, Func<int, string> getLine, Func<int, string> getJapaneseLine = null, ReportTimeContext context = null)
        {
            original = config; this.context = context; Text = "PoiTime 设置"; Font = new Font("微软雅黑", 9);
            ClientSize = new Size(600, 590); FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen; MaximizeBox = false;
            var tabs = new TabControl { Location = new Point(16, 16), Size = new Size(568, 480) };
            var packs = new TabPage("语音包"); var layout = new TabPage("区域与位置"); var audio = new TabPage("音频输出"); var appearance = new TabPage("立绘与台词"); var dialogue = new TabPage("编辑台词"); var typography = new TabPage("文字样式");
            tabs.TabPages.AddRange(new[] { packs, audio, appearance, layout, typography, dialogue }); Controls.Add(tabs);
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
            LabelAt(audio, "OBS 浏览器源勾选「通过 OBS 控制音频」。\n浏览器源宽高设置为实际展示区域的大小，\n尺寸见「区域与位置」页，可在 OBS 中直接拖动源。\n双输出时 OBS 监听保持关闭，避免重复发声。", 16, 250);

            desktopBox = Check(appearance, "桌面显示", 16, 16, config.showAnnouncement);
            browserBox = Check(appearance, "浏览器显示", 230, 16, config.browserShowAnnouncement);
            portraitBox = Check(appearance, "显示立绘", 16, 53, config.showPortrait);
            nameBox = Check(appearance, "显示名字", 160, 53, config.showName);
            lineBox = Check(appearance, "显示台词", 305, 53, config.showLine);
            LabelAt(layout, "桌面锚点", 16, 20);
            positionBox = Combo(layout, 105, 16, 330, new object[] { "左上角", "右上角", "屏幕中央", "左下角", "右下角" });
            int position = Array.IndexOf(new[] { "TopLeft", "TopRight", "Center", "BottomLeft", "BottomRight" }, config.announcementPosition); positionBox.SelectedIndex = position < 0 ? 4 : position;
            LabelAt(layout, "区域宽度", 16, 70); regionWidthBox = Number(layout, 125, 66, 240, 1200, config.regionWidth);
            LabelAt(layout, "区域高度", 16, 115); regionHeightBox = Number(layout, 125, 111, 0, 1200, config.regionHeight);
            LabelAt(layout, "像素；0 = 根据内容自动计算", 230, 115);
            LabelAt(layout, "整体缩放", 16, 160); scaleBox = Number(layout, 125, 156, 25, 300, config.regionScale); LabelAt(layout, "%（立绘、台词与背景一起缩放）", 230, 160);
            LabelAt(layout, "桌面水平偏移", 16, 215); offsetXBox = Number(layout, 125, 211, -10000, 10000, config.positionOffsetX);
            LabelAt(layout, "桌面垂直偏移", 16, 260); offsetYBox = Number(layout, 125, 256, -10000, 10000, config.positionOffsetY);
            LabelAt(layout, "正值向右 / 向下，负值向左 / 向上。", 16, 303);
            var regionSummary = LabelAt(layout, "", 16, 345);
            Action regionInfo = () => {
                if (languageBox == null || lineEditor == null || packBox == null) return;
                string name = "", portrait = "";
                if (context != null) { try { var pack = context.ReadVoicePack(SelectedPack); name = pack.name; portrait = Path.Combine(context.VoicePackPath(SelectedPack), Path.GetFileName(pack.portrait ?? "portrait.png")); } catch { } }
                var size = AnnouncementPopup.MeasureRegion(name, VoiceAnnouncement.FormatText(lineEditor.Text, japaneseEditor.Text, Draft.dialogueLanguage), portrait, Draft);
                regionSummary.Text = "当前试听区域：" + size.Width + " × " + size.Height + " 像素（OBS 源宽高）";
            };
            regionWidthBox.ValueChanged += (s, e) => regionInfo(); regionHeightBox.ValueChanged += (s, e) => { if (regionHeightBox.Value > 0 && regionHeightBox.Value < 100) regionHeightBox.Value = 100; regionInfo(); }; scaleBox.ValueChanged += (s, e) => regionInfo();
            LabelAt(layout, "网页只输出展示区域；在 OBS 源属性中填写上述宽高。\n自动高度随台词变化；OBS 可设固定高度以容纳较长台词。", 16, 385);
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
            var fontButton = new Button { Text = "设置字体、字号…", Location = new Point(16, 20), Size = new Size(240, 32) };
            var fontSummary = LabelAt(typography, "", 16, 65);
            Action summary = () => fontSummary.Text = fontFamily + "  " + fontSize.ToString("0.#") + " 磅" + (fontBold ? "  粗体" : "") + (fontItalic ? "  斜体" : "");
            summary(); fontButton.Click += (s, e) => { using (var font = new Font(fontFamily, fontSize, (fontBold ? FontStyle.Bold : FontStyle.Regular) | (fontItalic ? FontStyle.Italic : FontStyle.Regular))) using (var dialog = new FontDialog { Font = font, MinSize = 8, MaxSize = 32, FontMustExist = true, ShowEffects = false }) { if (dialog.ShowDialog(this) != DialogResult.OK) return; fontFamily = dialog.Font.Name; fontSize = dialog.Font.SizeInPoints; fontBold = dialog.Font.Bold; fontItalic = dialog.Font.Italic; summary(); } };
            LabelAt(typography, "文字颜色", 16, 115);
            var textButton = ColorButton(typography, textColor, 125, 108, value => textColor = value);
            outlineBox = Check(typography, "启用文字描边", 16, 172, config.textOutline);
            outlineColor = ParseColor(config.textOutlineColor, Color.Black);
            LabelAt(typography, "描边颜色", 16, 218);
            var outlineButton = ColorButton(typography, outlineColor, 125, 211, value => outlineColor = value);
            LabelAt(typography, "描边粗细", 16, 274);
            float stroke = float.IsNaN(config.textOutlineWidth) || float.IsInfinity(config.textOutlineWidth) ? 2 : Math.Max(.5f, Math.Min(8, config.textOutlineWidth));
            outlineWidth = new NumericUpDown { Minimum = .5m, Maximum = 8, DecimalPlaces = 1, Increment = .5m, Value = (decimal)stroke, Location = new Point(125, 270), Size = new Size(90, 26) };
            LabelAt(typography, "像素（0.5–8）", 228, 274);
            Action updateOutline = () => outlineButton.Enabled = outlineWidth.Enabled = outlineBox.Checked;
            outlineBox.CheckedChanged += (s, e) => updateOutline(); updateOutline();
            LabelAt(typography, "文字颜色和描边同时应用于名字与台词。\n透明背景建议保留描边，可用「试听 / 预览」查看效果。", 16, 330);
            typography.Controls.AddRange(new Control[] { fontButton, outlineWidth });
            LabelAt(appearance, "字体、文字颜色和描边请在「文字样式」页设置。\n桌面与网页共用外观；透明度仅作用于背景。", 16, 315);
            appearance.Controls.AddRange(new Control[] { color, imagePathBox, browse, opacitySlider });
            Action update = () => { imagePathBox.Enabled = browse.Enabled = backgroundBox.SelectedIndex == 1; color.Enabled = backgroundBox.SelectedIndex == 0; };
            backgroundBox.SelectedIndexChanged += (s, e) => update(); update();

            LabelAt(dialogue, "台词语言", 16, 20);
            languageBox = Combo(dialogue, 105, 16, 330, new object[] { "中文", "日文", "双语对照（中文 + 日文）" });
            int language = Array.IndexOf(new[] { "Chinese", "Japanese", "Bilingual" }, config.dialogueLanguage); languageBox.SelectedIndex = language < 0 ? 0 : language;
            LabelAt(dialogue, "缺少一种语言时显示已有台词。旧语音包可在「语音包」页\n补全立绘和台词获取日文；保存后生效。", 16, 58);
            LabelAt(dialogue, "中文台词（下方所选时刻）", 16, 107);
            lineEditor = new TextBox { Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical, Location = new Point(16, 133), Size = new Size(420, 125), Enabled = getLine != null };
            LabelAt(dialogue, "日文台词", 16, 269);
            japaneseEditor = new TextBox { Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical, Location = new Point(16, 295), Size = new Size(420, 125), Enabled = getJapaneseLine != null };
            dialogue.Controls.AddRange(new Control[] { lineEditor, japaneseEditor });
            LabelAt(this, "试听时刻", 16, 520);
            hour = new NumericUpDown { Minimum = 0, Maximum = 23, Value = DateTime.Now.Hour, Location = new Point(105, 516), Size = new Size(60, 26) };
            Action bind = () => { bindingLine = true; int h = (int)hour.Value; lineEditor.Text = EditedLines.TryGetValue(h, out var text) ? text : getLine?.Invoke(h) ?? ""; japaneseEditor.Text = EditedJapaneseLines.TryGetValue(h, out var jp) ? jp : getJapaneseLine?.Invoke(h) ?? ""; bindingLine = false; };
            hour.ValueChanged += (s, e) => bind(); lineEditor.TextChanged += (s, e) => { if (bindingLine || getLine == null) return; int h = (int)hour.Value; if (lineEditor.Text == (getLine(h) ?? "")) EditedLines.Remove(h); else EditedLines[h] = lineEditor.Text; }; japaneseEditor.TextChanged += (s, e) => { if (bindingLine || getJapaneseLine == null) return; int h = (int)hour.Value; if (japaneseEditor.Text == (getJapaneseLine(h) ?? "")) EditedJapaneseLines.Remove(h); else EditedJapaneseLines[h] = japaneseEditor.Text; }; bind();
            packBox = Combo(packs, 105, 16, 420, new object[0]); LabelAt(packs, "本地语音包", 16, 20);
            randomBox = Check(packs, "随机语音包（保存后生效）", 16, 60, config.randomVoices);
            var idBox = new TextBox { Text = "144", Location = new Point(105, 110), Size = new Size(110, 26) }; LabelAt(packs, "下载 ID", 16, 114);
            var wikiName = new TextBox { Location = new Point(105, 155), Size = new Size(420, 26) }; LabelAt(packs, "Wiki 页名称", 16, 159);
            var download = new Button { Text = "下载语音包", Location = new Point(16, 200), Size = new Size(140, 32) };
            var enrich = new Button { Text = "补全当前立绘与双语台词", Location = new Point(175, 200), Size = new Size(240, 32) };
            var progress = new ProgressBar { Location = new Point(16, 250), Size = new Size(510, 20), Maximum = 24 };
            var status = new Label { Text = "选择语音包后可编辑台词、试听；点击保存应用。", Location = new Point(16, 285), Size = new Size(510, 65) };
            var wikiLink = new LinkLabel { Text = "打开舰娘百科 Wiki", AutoSize = true, Location = new Point(16, 360) };
            wikiLink.LinkClicked += (s, e) => { string address = "https://zh.kcwiki.cn/" + (string.IsNullOrWhiteSpace(wikiName.Text) ? "" : "wiki/" + Uri.EscapeDataString(wikiName.Text.Trim())); try { Process.Start(new ProcessStartInfo(address) { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show(this, "无法打开浏览器：" + ex.Message); } };
            LabelAt(packs, "下载与补全会立即写入语音包；设置、切换和台词编辑在保存后生效。", 16, 405);
            packs.Controls.AddRange(new Control[] { idBox, wikiName, download, enrich, progress, status, wikiLink });
            Action<string> refreshPacks = selected => { bindingLine = true; packBox.Items.Clear(); packIds.Clear(); if (context != null) foreach (var pair in context.GetAvailableVoicePacks()) { packIds.Add(pair.Key); packBox.Items.Add(pair.Key + " · " + pair.Value); } packBox.SelectedIndex = packIds.Count == 0 ? -1 : Math.Max(0, packIds.IndexOf(selected)); bindingLine = false; };
            if (context != null) {
                getLine = h => PackLine(h, false); getJapaneseLine = h => PackLine(h, true); lineEditor.Enabled = japaneseEditor.Enabled = true;
                refreshPacks(context.CurrentVoicePackId);
                Action selectedPack = () => { try { if (context != null && packBox.SelectedIndex >= 0) { wikiName.Text = context.ReadVoicePack(SelectedPack).name ?? ""; idBox.Text = SelectedPack; bind(); } } catch (Exception ex) { status.Text = ex.Message; } };
                packBox.SelectedIndexChanged += (s, e) => { if (!bindingLine) selectedPack(); }; selectedPack();
                Func<Func<Task>, Task> run = async action => { busy = true; tabs.Enabled = false; hour.Enabled = false; try { await action(); bind(); } catch (Exception ex) { status.Text = "操作失败：" + ex.Message; } finally { busy = false; tabs.Enabled = true; hour.Enabled = true; } };
                download.Click += async (s, e) => { string id = idBox.Text.Trim(), name = wikiName.Text.Trim(); if (name.Length == 0) { status.Text = "请输入 Wiki 页名称。"; return; } await run(async () => { string result = await context.DownloadPackInSettingsAsync(id, name, (value, text) => { progress.Value = value; status.Text = text; }); packCache.Remove(id); refreshPacks(id); selectedPack(); status.Text = "下载完成。" + result + " 选择后试听，保存即可切换。"; }); };
                enrich.Click += async (s, e) => { string id = SelectedPack, name = wikiName.Text.Trim(); if (name.Length == 0) { status.Text = "请输入 Wiki 页名称。"; return; } await run(async () => { status.Text = "正在补全立绘和双语台词…"; status.Text = await context.TryEnrichVoicePackAsync(id, name, context.VoicePackPath(id)); packCache.Remove(id); }); };
            } else { packBox.Enabled = randomBox.Enabled = download.Enabled = enrich.Enabled = false; }
            languageBox.SelectedIndexChanged += (s, e) => regionInfo(); hour.ValueChanged += (s, e) => regionInfo();
            lineEditor.TextChanged += (s, e) => regionInfo(); japaneseEditor.TextChanged += (s, e) => regionInfo();
            packBox.SelectedIndexChanged += (s, e) => regionInfo(); fontButton.Click += (s, e) => regionInfo();
            portraitBox.CheckedChanged += (s, e) => regionInfo(); nameBox.CheckedChanged += (s, e) => regionInfo(); lineBox.CheckedChanged += (s, e) => regionInfo(); outlineBox.CheckedChanged += (s, e) => regionInfo(); outlineWidth.ValueChanged += (s, e) => regionInfo(); regionInfo();
            FormClosing += (s, e) => { if (busy) { e.Cancel = true; status.Text = "正在下载或补全，请等待完成后关闭。"; } };
            var play = new Button { Text = "试听 / 预览", Location = new Point(185, 513), Size = new Size(120, 30) };
            play.Click += (s, e) => { if (busy) return; preview((int)hour.Value, Draft, getLine == null ? null : VoiceAnnouncement.FormatText(lineEditor.Text, japaneseEditor.Text, Draft.dialogueLanguage)); };
            var stopButton = new Button { Text = "停止", Location = new Point(320, 513), Size = new Size(80, 30) }; stopButton.Click += (s, e) => stop();
            var save = new Button { Text = "保存", Location = new Point(415, 551), Size = new Size(75, 28) };
            save.Click += (s, e) => { if (busy) return; if ((Draft.showAnnouncement || Draft.browserShowAnnouncement) && Draft.backgroundMode == "Image") { try { using (var image = Image.FromFile(Draft.backgroundImage)) { } } catch (Exception ex) { MessageBox.Show(this, "请选择可读取的背景图片：" + ex.Message, Text); return; } } DialogResult = DialogResult.OK; };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(509, 551), Size = new Size(75, 28) };
            Controls.AddRange(new Control[] { hour, play, stopButton, save, cancel }); AcceptButton = save; CancelButton = cancel;
            FormClosed += (s, e) => stop();
        }
        private static NumericUpDown Number(Control parent, int x, int y, int min, int max, int value) { var number = new NumericUpDown { Location = new Point(x, y), Size = new Size(95, 26), Minimum = min, Maximum = max, Value = Math.Max(min, Math.Min(max, value)) }; parent.Controls.Add(number); return number; }
        private Button ColorButton(Control parent, Color initial, int x, int y, Action<Color> changed)
        {
            var button = new Button { Location = new Point(x, y), Size = new Size(230, 32) };
            Color selected = initial;
            Action refresh = () => { button.Text = "选择颜色…  " + ColorTranslator.ToHtml(selected); button.BackColor = selected; button.ForeColor = selected.GetBrightness() < .5 ? Color.White : Color.Black; };
            refresh(); button.Click += (s, e) => { using (var dialog = new ColorDialog { Color = selected, FullOpen = true }) { if (dialog.ShowDialog(this) != DialogResult.OK) return; selected = dialog.Color; changed(selected); refresh(); } };
            parent.Controls.Add(button); return button;
        }
        private static Color ParseColor(string value, Color fallback) { try { return ColorTranslator.FromHtml(value); } catch { return fallback; } }
        private static CheckBox Check(Control parent, string text, int x, int y, bool value) { var box = new CheckBox { Text = text, Checked = value, AutoSize = true, Location = new Point(x, y) }; parent.Controls.Add(box); return box; }
        private static ComboBox Combo(Control parent, int x, int y, int width, object[] items) { var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(x, y), Size = new Size(width, 26) }; combo.Items.AddRange(items); parent.Controls.Add(combo); return combo; }
        private static Label LabelAt(Control parent, string text, int x, int y) { var label = new Label { Text = text, AutoSize = true, Location = new Point(x, y) }; parent.Controls.Add(label); return label; }
    }
}
