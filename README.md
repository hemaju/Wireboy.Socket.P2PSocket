# Wireboy.Socket.P2PSocket

一个跨平台的 P2P 内网穿透项目（支持 Windows、Linux、树莓派等操作系统），类似花生壳：将内网网站、数据库、远程桌面等服务经 **P2P 打洞直连隧道** 暴露到公网，不同内网的电脑可互相访问。

> **4.0 版本已迁移至新仓库进行重构开发：[P2PSocket_AI](https://github.com/bobowire/P2PSocket_AI)**
> 本仓库的 master / dev 分支继续维护 3.x 稳定版本（.Net Core 2.1 + .Net Standard 2.0），使用说明见 [master 分支 README](https://github.com/bobowire/Wireboy.Socket.P2PSocket/tree/master)。

## 4.0 版本简介（P2PSocket_AI）

4.0 版本基于原项目重新设计，升级为一个可自部署的 P2P 内网穿透产品，主要特性：

1. **控制面集中、数据面 P2P**：服务端只负责注册/授权/信令/STUN/中继兜底，业务流量端到端直连，不经过服务端（打洞失败时可回退内置中继）
2. **全链路端到端加密**：隧道采用 ECDH(P-256)+HKDF+AES-256-GCM，控制通道采用 HMAC-SHA256 防重放，不依赖 TLS
3. **双端 Web 管理界面**：客户端本地 Web + 服务端 Web 后台（Vue 3 + Element Plus），支持简易/高级双模式
4. **跨平台支持**：.NET 10 + 虚拟网卡（Windows Wintun / Linux TUN），提供 Windows 安装包、Linux tar 与 Docker 部署

## 当前进度

| 里程碑 | 内容 | 状态 |
|---|---|---|
| M1 核心链路 | 设备注册 + 首条 UDP 直连映射 | ✅ 收官（2026-09-12） |
| M2 打洞完整与授权闭环 | TCP 打洞、中继回退、分组/远程码 | 🔨 35/38（余项待实机环境） |
| M3 管理面与可观测 | 服务端 Web 后台、仪表盘、统计 | 🔨 17/18（余项待实机验证） |
| M4 交付打磨 | 安装包、Docker、基准报告、交付文档 | ✅ 收官（2026-10-10） |
| M5 安全加固 | 安全修复、告警/安全中心/IP 黑名单 | ✅ 收官（16/16，2026-10-22~24） |

当前处于**收官后维护期**，缺陷修复、功能增补与加固迭代持续进行中。

> 详细进度、产品文档与构建部署说明请前往新仓库查看：**https://github.com/bobowire/P2PSocket_AI**

## 交流

在使用过程中遇到问题，可加入 QQ 群 417159195，与作者共同交流，入群请填写"P2PSocket"
