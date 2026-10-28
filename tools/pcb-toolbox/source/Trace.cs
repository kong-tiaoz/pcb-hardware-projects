using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

// Independent implementation of Lazar Rozenblat's published Fig. 5-2 curve fit.
// https://www.smps.us/pcb-calculator.html (not the IPC-2221 formula).
internal static class TraceMath
{
    internal static double Factor(double rise) { return 117.555 * Math.Pow(rise, -0.913) + 1.15; }
    internal static double Exponent(double rise) { return 0.84 * Math.Pow(rise, -0.108) + 1.159; }
    internal static double Area(double current, double rise)
    {
        if (current <= 0 || current > 30 || rise < 2 || rise > 100)
            throw new ArgumentOutOfRangeException();
        return Factor(rise) * Math.Pow(current, Exponent(rise));
    }
    internal static double Width(double current, double rise, double thicknessUm)
    {
        if (thicknessUm <= 0) throw new ArgumentOutOfRangeException();
        return Area(current, rise) / (thicknessUm / 25.4);
    }
    internal static double Current(double widthMil, double rise, double thicknessUm)
    {
        if (widthMil <= 0 || thicknessUm <= 0 || rise < 2 || rise > 100)
            throw new ArgumentOutOfRangeException();
        return Math.Pow(widthMil * thicknessUm / 25.4 / Factor(rise), 1 / Exponent(rise));
    }
}

internal sealed class MainForm : Form
{
    internal MainForm()
    {
        Text = "PCB 小工具 · MIL/mm 与走线载流";
        Font = new Font("Microsoft YaHei UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(600, 618);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        TabControl tabs = new TabControl();
        tabs.Dock = DockStyle.Fill;
        tabs.Padding = new Point(18, 10);
        TabPage trace = new TabPage("IPC-2152 走线载流");
        TabPage units = new TabPage("MIL / mm 单位转换");
        trace.Controls.Add(new TracePanel { Dock = DockStyle.Fill });
        units.Controls.Add(new ConverterForm { Dock = DockStyle.Fill });
        tabs.TabPages.Add(trace);
        tabs.TabPages.Add(units);
        Controls.Add(tabs);
    }
}

internal sealed class TracePanel : UserControl
{
    private ComboBox mode = new ComboBox();
    private ComboBox preset = new ComboBox();
    private ComboBox unit = new ComboBox();
    private NumericUpDown input = new NumericUpDown();
    private NumericUpDown rise = new NumericUpDown();
    private NumericUpDown copper = new NumericUpDown();
    private Label inputLabel;
    private Label result;
    private Label details;

    internal TracePanel()
    {
        BackColor = Color.FromArgb(247,249,252);
        AddLabel("走线载流计算", 22, 16, 520, 34).Font = new Font("Microsoft YaHei UI", 17, FontStyle.Bold);
        AddLabel("IPC-2152 图 5-2 · 基准曲线拟合估算", 24, 58, 530, 26);
        AddLabel("计算方式", 24, 105, 155, 28);
        SetupCombo(mode, 190, 100, 365, new string[] { "按电流计算所需线宽", "按线宽估算持续载流" });
        inputLabel = AddLabel("持续电流（A）", 24, 156, 160, 28);
        SetupNumber(input, 190, 150, 240, 0.001m, 30m, 3m, 3);
        SetupCombo(unit, 440, 150, 115, new string[] { "mm", "MIL" });
        unit.Visible = false;
        AddLabel("允许温升（℃）", 24, 207, 160, 28);
        SetupNumber(rise, 190, 201, 365, 2m, 100m, 10m, 1);
        AddLabel("铜厚预设", 24, 258, 160, 28);
        SetupCombo(preset, 190, 252, 365, new string[] { "1 oz ≈ 35 μm", "0.5 oz ≈ 17.5 μm", "2 oz ≈ 70 μm", "3 oz ≈ 105 μm", "自定义成品铜厚" });
        AddLabel("成品铜厚（μm）", 24, 309, 165, 28);
        SetupNumber(copper, 190, 303, 365, 1m, 500m, 35m, 2);
        copper.Enabled = false;
        result = AddLabel("", 24, 357, 535, 40);
        result.Font = new Font("Segoe UI", 19, FontStyle.Bold);
        result.ForeColor = Color.FromArgb(30,64,120);
        details = AddLabel("", 24, 401, 535, 48);
        details.Font = new Font("Microsoft YaHei UI", 9F);
        Label note = AddLabel("基准条件：静止空气，约 1.78 mm 厚聚酰亚胺板、3 oz 测试曲线。\n不含板厚、铜平面、相邻热源及铜厚热修正；内外层共用基准。\n电流 ≤ 30 A，温升 2–100℃；结果未加裕量，投板前应核对温升。", 24, 455, 535, 68);
        note.Font = new Font("Microsoft YaHei UI", 9F);
        note.ForeColor = Color.FromArgb(95,107,125);
        LinkLabel source = new LinkLabel();
        source.Text = "算法与适用条件";
        source.SetBounds(24, 533, 200, 27);
        source.LinkClicked += delegate { MessageBox.Show(this,
            "采用 Lazar Rozenblat 发布的 IPC-2152 图 5-2 通用拟合：\n\nA = (117.555 × ΔT^(-0.913) + 1.15)\n      × I^(0.84 × ΔT^(-0.108) + 1.159)\n\nA：铜截面积（MIL²），I：持续电流或 RMS 电流（A）\n线宽 = A ÷ 成品铜厚（MIL）。反向按同一公式求电流。\n\n本工具只实现基准曲线估算，不是 IPC 完整修正模型或认证。\n铜厚预设为近似值，请优先输入板厂成品铜厚。\n\n公式来源：https://www.smps.us/pcb-calculator.html\n标准：https://www.ipc.org/TOC/IPC-2152.pdf",
            "算法说明", MessageBoxButtons.OK, MessageBoxIcon.Information); };
        Controls.Add(source);
        mode.SelectedIndexChanged += delegate {
            bool widthMode = mode.SelectedIndex == 1;
            inputLabel.Text = widthMode ? "已有线宽" : "持续电流（A）";
            unit.Visible = widthMode;
            input.Maximum = widthMode ? 10000m : 30m;
            input.Value = widthMode ? 1m : 3m;
            Recalculate();
        };
        preset.SelectedIndexChanged += delegate {
            copper.Enabled = preset.SelectedIndex == 4;
            decimal[] values = {35m,17.5m,70m,105m};
            if (preset.SelectedIndex < 4) copper.Value = values[preset.SelectedIndex];
            Recalculate();
        };
        unit.SelectedIndexChanged += delegate {
            decimal width = unit.SelectedIndex == 1 ? input.Value / 0.0254m : input.Value * 0.0254m;
            input.Value = Math.Max(input.Minimum, Math.Min(input.Maximum, width));
            Recalculate();
        };
        input.ValueChanged += delegate { Recalculate(); };
        rise.ValueChanged += delegate { Recalculate(); };
        copper.ValueChanged += delegate { Recalculate(); };
        Recalculate();
    }

    private Label AddLabel(string text, int x, int y, int w, int h)
    {
        Label label = new Label { Text = text };
        label.SetBounds(x,y,w,h);
        Controls.Add(label);
        return label;
    }
    private void SetupCombo(ComboBox box, int x, int y, int w, string[] values)
    {
        box.DropDownStyle = ComboBoxStyle.DropDownList;
        box.SetBounds(x,y,w,34);
        box.Items.AddRange(values);
        box.SelectedIndex = 0;
        Controls.Add(box);
    }
    private void SetupNumber(NumericUpDown box, int x, int y, int w, decimal min, decimal max, decimal value, int places)
    {
        box.Minimum = min; box.Maximum = max; box.DecimalPlaces = places;
        box.Value = value; box.Increment = places == 3 ? 0.1m : 1m;
        box.Font = new Font("Segoe UI", 12);
        box.SetBounds(x,y,w,34);
        Controls.Add(box);
    }
    private void Recalculate()
    {
        double dt = (double)rise.Value, thick = (double)copper.Value;
        if (mode.SelectedIndex == 0)
        {
            double mil = TraceMath.Width((double)input.Value, dt, thick);
            result.Text = String.Format(CultureInfo.InvariantCulture, "{0:0.0000} mm  /  {1:0.000} MIL", mil * 0.0254, mil);
            details.Text = "基准所需线宽（未加裕量）；布线时请向上取整。\n温升指走线温度高于周围环境的温差。";
        }
        else
        {
            double mil = (double)input.Value / (unit.SelectedIndex == 0 ? 0.0254 : 1);
            double amps = TraceMath.Current(mil, dt, thick);
            result.Text = amps > 30 ? "超出 30 A 模型范围" : String.Format(CultureInfo.InvariantCulture, "估算载流：{0:0.000} A", amps);
            details.Text = String.Format(CultureInfo.InvariantCulture, "已有线宽：{0:0.0000} mm / {1:0.000} MIL\n{2}", mil * 0.0254, mil,
                amps > 30 ? "该尺寸结果超出基准范围，不显示外推载流值。" : "持续 / RMS 电流估算，未加裕量。");
        }
    }
}
