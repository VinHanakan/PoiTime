using System;
using System.Drawing;
using System.Windows.Forms;

namespace ReportTimePlayer
{
    public sealed class AnnouncementSettingsForm : Form
    {
        private readonly TrackBar volumeSlider;
        private readonly CheckBox showAnnouncementBox;
        private readonly ComboBox positionBox;

        public int Volume => volumeSlider.Value;
        public bool ShowAnnouncement => showAnnouncementBox.Checked;
        public string AnnouncementPosition => ((PositionOption)positionBox.SelectedItem).Value;

        public AnnouncementSettingsForm(Config config)
        {
            Text = "报时设置";
            Font = new Font("微软雅黑", 9);
            ClientSize = new Size(350, 215);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;

            var volumeLabel = new Label { Text = "音量", Location = new Point(16, 18), AutoSize = true };
            var valueLabel = new Label { Location = new Point(290, 18), AutoSize = true };
            volumeSlider = new TrackBar
            {
                Minimum = 0, Maximum = 100, TickFrequency = 10,
                Value = Math.Max(0, Math.Min(100, config.volume)),
                Location = new Point(16, 43), Size = new Size(315, 45)
            };
            valueLabel.Text = volumeSlider.Value + "%";
            volumeSlider.ValueChanged += (sender, args) => valueLabel.Text = volumeSlider.Value + "%";

            showAnnouncementBox = new CheckBox
            {
                Text = "报时显示立绘与台词", Checked = config.showAnnouncement,
                Location = new Point(16, 94), AutoSize = true
            };
            var positionLabel = new Label { Text = "桌面位置", Location = new Point(16, 130), AutoSize = true };
            positionBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(94, 126), Size = new Size(220, 26)
            };
            positionBox.Items.AddRange(new object[]
            {
                new PositionOption("左上", "TopLeft"),
                new PositionOption("右上", "TopRight"),
                new PositionOption("正中", "Center"),
                new PositionOption("左下", "BottomLeft"),
                new PositionOption("右下", "BottomRight")
            });
            for (int i = 0; i < positionBox.Items.Count; i++)
            {
                if (((PositionOption)positionBox.Items[i]).Value == config.announcementPosition)
                    positionBox.SelectedIndex = i;
            }
            if (positionBox.SelectedIndex < 0) positionBox.SelectedIndex = 4;
            positionBox.Enabled = showAnnouncementBox.Checked;
            showAnnouncementBox.CheckedChanged += (sender, args) => positionBox.Enabled = showAnnouncementBox.Checked;

            var saveButton = new Button
            {
                Text = "保存", DialogResult = DialogResult.OK,
                Location = new Point(159, 173), Size = new Size(75, 28)
            };
            var cancelButton = new Button
            {
                Text = "取消", DialogResult = DialogResult.Cancel,
                Location = new Point(245, 173), Size = new Size(75, 28)
            };
            Controls.AddRange(new Control[]
            {
                volumeLabel, valueLabel, volumeSlider, showAnnouncementBox,
                positionLabel, positionBox, saveButton, cancelButton
            });
            AcceptButton = saveButton;
            CancelButton = cancelButton;
        }

        private sealed class PositionOption
        {
            public string Label { get; }
            public string Value { get; }
            public PositionOption(string label, string value) { Label = label; Value = value; }
            public override string ToString() => Label;
        }
    }
}
