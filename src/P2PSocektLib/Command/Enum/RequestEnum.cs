using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Command
{
    /// <summary>
    /// 服务端-客户端通讯命令
    /// </summary>
    public enum RequestEnum : byte
    {
        心跳 = 3,
        版本检测,
        客户端认证,
        辅助认证,
        延迟检测,
        Nat类型检测,
        数据同步,

        端口映射_权限认证 = 20,
        管道_申请3端管道,
        管道_通知建立管道,
        管道_管道建立,
        管道_通知握手完成,

        管道P2P_申请握手,
        管道P2P_通知双方握手,
        管道P2P_通知开始打洞,
        管道P2P_完成校验,
        管道_转发数据,

        获取客户端列表 = 60,
        获取本机外网端口,

        管理员登录 = 100,
        获取在线客户端,
        获取服务配置,
        修改服务配置,
        重启服务,
        查询tcp数量,
        查询账号,
        添加账号,
        删除账号,
        重置账号密码,
        扩展命令 = 245
    }
}
