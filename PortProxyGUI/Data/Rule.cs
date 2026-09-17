using System;

namespace PortProxyGUI.Data;

public class Rule : IEquatable<Rule>
{
    public string Id { get; set; }

    public string Type { get; set; }
    public string ListenOn { get; set; }
    public int ListenPort { get; set; }
    public string ConnectTo { get; set; }
    public int ConnectPort { get; set; }
    public string Comment { get; set; }
    public string Group { get; set; }

    public bool Valid => ListenPort > 0 && ConnectPort > 0;

    private string _realListenPort;
    /// <summary>
    /// Not mapped
    /// </summary>
    public string RealListenPort
    {
        get => ListenPort > 0 ? ListenPort.ToString() : _realListenPort;
        set => _realListenPort = value;
    }

    private string _realConnectPort;
    /// <summary>
    /// Not mapped
    /// </summary>
    public string RealConnectPort
    {
        get => ConnectPort > 0 ? ConnectPort.ToString() : _realConnectPort;
        set => _realConnectPort = value;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (Id?.GetHashCode() ?? 0);
            hash = hash * 31 + (Type?.GetHashCode() ?? 0);
            hash = hash * 31 + (ListenOn?.GetHashCode() ?? 0);
            hash = hash * 31 + ListenPort.GetHashCode();
            hash = hash * 31 + (ConnectTo?.GetHashCode() ?? 0);
            hash = hash * 31 + ConnectPort.GetHashCode();
            hash = hash * 31 + (Comment?.GetHashCode() ?? 0);
            hash = hash * 31 + (Group?.GetHashCode() ?? 0);
            return hash;
        }
    }

    public bool Equals(Rule other)
    {
        return other is not null
            && Id == other.Id
            && Type == other.Type
            && ListenOn == other.ListenOn
            && ListenPort == other.ListenPort
            && ConnectTo == other.ConnectTo
            && ConnectPort == other.ConnectPort
            && Comment == other.Comment
            && Group == other.Group;
    }

    public bool EqualsWithKeys(Rule other)
    {
        return other is not null
            && Type == other.Type
            && ListenOn == other.ListenOn
            && ListenPort == other.ListenPort;
    }

    public static int ParsePort(string portString)
    {
        if (int.TryParse(portString, out var port) && 0 < port && port < 65536) return port;
        else throw new NotSupportedException($"端口无效：{portString}。请输入 1 到 65535 之间的数字。");
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as Rule);
    }
}
