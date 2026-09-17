using PortProxyGUI.Native;
using System;

namespace PortProxyGUI.Utils;

internal class DnsUtil
{
    public static void FlushCache()
    {
        var status = NativeMethods.DnsFlushResolverCache();
        if (status == 0) throw new InvalidOperationException("清除 DNS 缓存失败。");
    }

}
