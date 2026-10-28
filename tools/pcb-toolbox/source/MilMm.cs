using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

internal static class Program
{
    internal const decimal MmPerMil = 0.0254m;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--self-test")
        {
            return 1m * MmPerMil == 0.0254m &&
                1000m * MmPerMil == 25.4m &&
                1m / MmPerMil > 39.37007874m &&
                -100m * MmPerMil == -2.54m &&
                0m / MmPerMil == 0m ? 0 : 1;
        }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
        return 0;
    }
}

internal sealed class ConverterForm : UserControl
{
    private readonly TextBox mil = new TextBox();
    private readonly TextBox mm = new TextBox();
    private readonly Label status = new Label();
    private bool updating;

    internal ConverterForm()
    {
        Text = "MIL / mm 单位转换器";
        ClientSize = new Size(420, 252);
        BackColor = Color.FromArgb(247, 249, 252);
        Font = new Font("Microsoft YaHei UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Label title = new Label();
        title.Text = "MIL  ↔  mm";
        title.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        title.ForeColor = Color.FromArgb(30, 64, 120);
        title.SetBounds(24, 15, 370, 43);
        Controls.Add(title);

        AddRow("MIL（密耳）", mil, 78);
        AddRow("mm（毫米）", mm, 131);
        mil.TabIndex = 0;
        mm.TabIndex = 1;
        mil.AccessibleName = "MIL 密耳";
        mm.AccessibleName = "mm 毫米";
        mil.TextChanged += delegate { ConvertValue(mil, mm, true); };
        mm.TextChanged += delegate { ConvertValue(mm, mil, false); };

        status.Text = "输入任一数值，即时双向换算";
        status.ForeColor = Color.FromArgb(95, 107, 125);
        status.SetBounds(24, 187, 375, 24);
        Controls.Add(status);
        Label rule = new Label();
        rule.Text = "1 MIL = 0.0254 mm     ·     1 MIL = 0.001 英寸";
        rule.Font = new Font("Microsoft YaHei UI", 9F);
        rule.ForeColor = Color.FromArgb(95, 107, 125);
        rule.SetBounds(24, 217, 380, 24);
        Controls.Add(rule);
    }

    private void AddRow(string label, TextBox input, int y)
    {
        Label unit = new Label();
        unit.Text = label;
        unit.SetBounds(24, y + 6, 112, 28);
        Controls.Add(unit);
        input.Font = new Font("Segoe UI", 14F);
        input.SetBounds(140, y, 255, 36);
        Controls.Add(input);
    }

    private void ConvertValue(TextBox source, TextBox target, bool fromMil)
    {
        if (updating) return;
        updating = true;
        try
        {
            if (String.IsNullOrWhiteSpace(source.Text))
            {
                target.Clear();
                status.Text = "输入任一数值，即时双向换算";
                return;
            }
            decimal value;
            if (!Decimal.TryParse(source.Text.Trim(), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out value))
            {
                target.Clear();
                status.Text = "请输入有效数字（小数点用 .）";
                return;
            }
            decimal result = fromMil ? value * Program.MmPerMil : value / Program.MmPerMil;
            target.Text = result.ToString("0.############################", CultureInfo.InvariantCulture);
            status.Text = "输入任一数值，即时双向换算";
        }
        catch (OverflowException)
        {
            target.Clear();
            status.Text = "数值过大，请输入较小的数字";
        }
        finally { updating = false; }
    }
}
