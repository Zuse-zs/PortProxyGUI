using NStandard;
using PortProxyGUI.Data;
using PortProxyGUI.Utils;
using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Windows.Forms;

namespace PortProxyGUI;

public partial class SetProxy : Form
{
    public readonly PortProxyGUI ParentWindow;
    private string AutoTypeString { get; }

    private bool _updateMode;
    private ListViewItem _listViewItem;
    private Rule _itemRule;
    private Label _flowPreviewLabel;

    public SetProxy(PortProxyGUI parent)
    {
        ParentWindow = parent;

        InitializeComponent();
        ConfigureExplanatoryLayout();

        AutoTypeString = comboBox_Type.Text = comboBox_Type.Items.OfType<string>().First();
        var groupNames = (
            from g in parent.listViewProxies.Groups.OfType<ListViewGroup>()
            let header = g.Header
            where !header.IsNullOrWhiteSpace()
            select header
        ).ToArray();
        comboBox_Group.Items.AddRange(groupNames);
    }

    private void ConfigureExplanatoryLayout()
    {
        Text = "设置 TCP 端口转发（Windows → 目标服务）";
        ClientSize = new Size(760, 475);

        var explanation = new Label
        {
            AutoSize = false,
            Location = new Point(20, 15),
            Size = new Size(720, 46),
            Text = "客户端不会直接连接 WSL 或目标服务。本规则先让客户端连接 Windows，\r\n" +
                   "再由 Windows 把 TCP 连接转发到下面填写的目标设备或服务。",
        };

        label_Type.Text = "IP 类型";
        label_Type.Location = new Point(20, 73);
        comboBox_Type.Location = new Point(100, 69);
        comboBox_Type.Size = new Size(190, 25);

        label_Group.Text = "规则分组";
        label_Group.Location = new Point(335, 73);
        comboBox_Group.Location = new Point(415, 69);
        comboBox_Group.Size = new Size(325, 25);

        var listenerGroup = new GroupBox
        {
            Location = new Point(20, 105),
            Size = new Size(720, 100),
            Text = "① Windows 本机（入口）：客户端连接这里",
        };
        label_ListenOn.Text = "监听 IP";
        label_ListenOn.Location = new Point(16, 31);
        textBox_ListenOn.Location = new Point(112, 27);
        textBox_ListenOn.Size = new Size(320, 23);
        label_ListenPort.Text = "监听端口";
        label_ListenPort.Location = new Point(455, 31);
        textBox_ListenPort.Location = new Point(570, 27);
        textBox_ListenPort.Size = new Size(125, 23);
        var listenerHint = new Label
        {
            AutoSize = false,
            ForeColor = Color.DimGray,
            Location = new Point(16, 59),
            Size = new Size(680, 30),
            Text = "0.0.0.0 / * = 所有网卡；10.126.126.16 = 仅监听本机 EasyTier 网卡。",
        };
        listenerGroup.Controls.AddRange(new Control[]
        {
            label_ListenOn, textBox_ListenOn, label_ListenPort, textBox_ListenPort, listenerHint,
        });

        var targetGroup = new GroupBox
        {
            Location = new Point(20, 215),
            Size = new Size(720, 100),
            Text = "② 目标设备/服务（出口）：Windows 把连接转发到这里",
        };
        label_ConnectTo.Text = "目标 IP";
        label_ConnectTo.Location = new Point(16, 31);
        textBox_ConnectTo.Location = new Point(112, 27);
        textBox_ConnectTo.Size = new Size(320, 23);
        label_ConnectPort.Text = "目标端口";
        label_ConnectPort.Location = new Point(455, 31);
        textBox_ConnectPort.Location = new Point(570, 27);
        textBox_ConnectPort.Size = new Size(125, 23);
        var targetHint = new Label
        {
            AutoSize = false,
            ForeColor = Color.DimGray,
            Location = new Point(16, 59),
            Size = new Size(680, 30),
            Text = "WSL：目标 IP 是 WSL 地址；目标端口是应用或容器在 WSL 内监听的端口。",
        };
        targetGroup.Controls.AddRange(new Control[]
        {
            label_ConnectTo, textBox_ConnectTo, label_ConnectPort, textBox_ConnectPort, targetHint,
        });

        var flowPanel = new Panel
        {
            BackColor = Color.FromArgb(239, 247, 255),
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(20, 325),
            Size = new Size(720, 80),
        };
        var flowTitle = new Label
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Location = new Point(10, 7),
            Text = "根据当前填写内容，实际连接过程如下",
        };
        _flowPreviewLabel = new Label
        {
            AutoEllipsis = true,
            AutoSize = false,
            ForeColor = Color.FromArgb(0, 82, 155),
            Location = new Point(10, 29),
            Size = new Size(698, 44),
        };
        flowPanel.Controls.AddRange(new Control[] { flowTitle, _flowPreviewLabel });

        label_Comment.Text = "用途/备注";
        label_Comment.Location = new Point(20, 439);
        textBox_Comment.Location = new Point(100, 435);
        textBox_Comment.Size = new Size(445, 23);
        button_Set.Text = "保存并启用";
        button_Set.Location = new Point(600, 429);
        button_Set.Size = new Size(140, 35);

        Controls.AddRange(new Control[] { explanation, listenerGroup, targetGroup, flowPanel });

        components ??= new System.ComponentModel.Container();
        var help = new ToolTip(components);
        help.SetToolTip(textBox_ListenOn, "客户端要连接的 Windows 本机 IP。0.0.0.0 表示所有网卡地址。");
        help.SetToolTip(textBox_ListenPort, "Windows 对外接收 TCP 连接的端口。");
        help.SetToolTip(textBox_ConnectTo, "Windows 收到连接后，要转发到的设备或服务 IP。");
        help.SetToolTip(textBox_ConnectPort, "目标设备或服务实际监听的 TCP 端口。");
        help.SetToolTip(comboBox_Type, "v4tov4 表示 IPv4 入口转发到 IPv4 目标；通常保留自动选择即可。");

        textBox_ListenOn.TextChanged += UpdateFlowPreview;
        textBox_ListenPort.TextChanged += UpdateFlowPreview;
        textBox_ConnectTo.TextChanged += UpdateFlowPreview;
        textBox_ConnectPort.TextChanged += UpdateFlowPreview;
        comboBox_Group.TextChanged += UpdateFlowPreview;
        UpdateFlowPreview(this, EventArgs.Empty);
    }

    private void UpdateFlowPreview(object sender, EventArgs e)
    {
        var listenAddress = textBox_ListenOn.Text.Trim();
        var listenPort = textBox_ListenPort.Text.Trim();
        if (listenPort.IsNullOrWhiteSpace()) listenPort = "监听端口";

        string windowsEndpoint;
        if (listenAddress == "0.0.0.0" || listenAddress == "*")
        {
            windowsEndpoint = $"本机可达 IP:{listenPort}（{listenAddress} 表示监听所有网卡）";
        }
        else
        {
            if (listenAddress.IsNullOrWhiteSpace()) listenAddress = "监听 IP";
            windowsEndpoint = $"{listenAddress}:{listenPort}";
        }

        var connectAddress = textBox_ConnectTo.Text.Trim();
        if (connectAddress.IsNullOrWhiteSpace()) connectAddress = "目标 IP";

        var connectPort = textBox_ConnectPort.Text.Trim();
        if (connectPort.IsNullOrWhiteSpace()) connectPort = "目标端口";

        var targetName = comboBox_Group.Text.Trim().Equals("WSL", StringComparison.OrdinalIgnoreCase)
            ? "WSL"
            : "目标服务";
        _flowPreviewLabel.Text = $"① 客户端连接 Windows：{windowsEndpoint}\r\n" +
                                 $"② Windows 再转发到 {targetName}：{connectAddress}:{connectPort}";
    }

    public void UseNormalMode()
    {
        _updateMode = false;
        _listViewItem = null;
        _itemRule = null;

        comboBox_Type.Text = AutoTypeString;
        comboBox_Group.Text = "";

        textBox_ListenOn.Text = "*";
        textBox_ListenPort.Text = "";
        textBox_ConnectTo.Text = "";
        textBox_ConnectPort.Text = "";
        textBox_Comment.Text = "";
    }

    public void UseWslMode(string address)
    {
        UseNormalMode();
        comboBox_Type.Text = "v4tov4";
        textBox_ListenOn.Text = "0.0.0.0";
        textBox_ConnectTo.Text = address;
        textBox_Comment.Text = "WSL 端口转发";
        comboBox_Group.Text = "WSL";
        textBox_ListenPort.Focus();
    }

    public void UseUpdateMode(ListViewItem item, Rule rule)
    {
        _updateMode = true;
        _listViewItem = item;

        _itemRule = rule;

        comboBox_Type.Text = rule.Type;
        comboBox_Group.Text = rule.Group;

        textBox_ListenOn.Text = rule.ListenOn;
        textBox_ListenPort.Text = rule.ListenPort.ToString();
        textBox_ConnectTo.Text = rule.ConnectTo;
        textBox_ConnectPort.Text = rule.ConnectPort.ToString();
        textBox_Comment.Text = rule.Comment;
    }

    private bool IsIPv6(string ip)
    {
        return IPAddress.TryParse(ip, out var address)
            && address.AddressFamily == AddressFamily.InterNetworkV6;
    }

    private static bool IsValidAddress(string address, bool allowWildcard)
    {
        return (allowWildcard && address == "*") || IPAddress.TryParse(address, out _);
    }

    private string GetPassType(string listenOn, string connectTo)
    {
        var from = IsIPv6(listenOn) ? "v6" : "v4";
        var to = IsIPv6(connectTo) ? "v6" : "v4";
        return $"{from}to{to}";
    }

    private void button_Set_Click(object sender, EventArgs e)
    {
        int listenPort, connectPort;

        try
        {
            listenPort = Rule.ParsePort(textBox_ListenPort.Text);
            connectPort = Rule.ParsePort(textBox_ConnectPort.Text);
        }
        catch (NotSupportedException ex)
        {
            MessageBox.Show(ex.Message, "端口无效", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }

        var rule = new Rule
        {
            Type = comboBox_Type.Text.Trim(),
            ListenOn = textBox_ListenOn.Text.Trim(),
            ListenPort = listenPort,
            ConnectTo = textBox_ConnectTo.Text.Trim(),
            ConnectPort = connectPort,
            Comment = textBox_Comment.Text.Trim(),
            Group = comboBox_Group.Text.Trim(),
        };

        if (!IsValidAddress(rule.ListenOn, true) || !IsValidAddress(rule.ConnectTo, false))
        {
            MessageBox.Show("请输入有效的监听地址和目标 IP 地址；监听地址可使用 *。", "地址无效",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }

        if (rule.Type == AutoTypeString) rule.Type = GetPassType(rule.ListenOn, rule.ConnectTo);

        if (!new[] { "v4tov4", "v4tov6", "v6tov4", "v6tov6" }.Contains(rule.Type))
        {
            MessageBox.Show($"无法确定从 {rule.ListenOn} 到 {rule.ConnectTo} 的转发类型。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }

        var oldRule = _updateMode
            ? Program.Database.GetRule(_itemRule.Type, _itemRule.ListenOn, _itemRule.ListenPort)
            : null;
        var conflictingRule = Program.Database.GetRule(rule.Type, rule.ListenOn, rule.ListenPort);
        if (conflictingRule is not null && (oldRule is null || conflictingRule.Id != oldRule.Id))
        {
            MessageBox.Show("相同类型、监听地址和端口的规则已经存在。", "规则冲突",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }

        if (_updateMode)
        {
            Util.AddOrUpdateProxy(rule);
            if (oldRule is not null)
            {
                if (!oldRule.EqualsWithKeys(rule)) Util.DeleteProxy(oldRule);
                Program.Database.Remove(oldRule);
            }
            Program.Database.Add(rule);

            ParentWindow.UpdateListViewItem(_listViewItem, rule, 1);
        }
        else
        {
            Util.AddOrUpdateProxy(rule);
            Program.Database.Add(rule);

            ParentWindow.RefreshProxyList();
        }
        Util.ParamChange();

        Close();
    }

    private void SetProxyForm_Load(object sender, EventArgs e)
    {
        Top = ParentWindow.Top + (ParentWindow.Height - Height) / 2;
        Left = ParentWindow.Left + (ParentWindow.Width - Width) / 2;
    }

    private void SetProxyForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        ParentWindow.SetProxyForm = null;
    }

}
