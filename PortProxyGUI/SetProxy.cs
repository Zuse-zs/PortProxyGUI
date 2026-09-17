using NStandard;
using PortProxyGUI.Data;
using PortProxyGUI.Utils;
using System;
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

    public SetProxy(PortProxyGUI parent)
    {
        ParentWindow = parent;

        InitializeComponent();

        AutoTypeString = comboBox_Type.Text = comboBox_Type.Items.OfType<string>().First();
        var groupNames = (
            from g in parent.listViewProxies.Groups.OfType<ListViewGroup>()
            let header = g.Header
            where !header.IsNullOrWhiteSpace()
            select header
        ).ToArray();
        comboBox_Group.Items.AddRange(groupNames);
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
