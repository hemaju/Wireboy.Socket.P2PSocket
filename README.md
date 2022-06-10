# Wireboy.Socket.P2PSocket

# 项目简介

这是一个开发中的4.0版本，本次开发将重新设计代码结构，使用.Net6框架，采用async...await进行开发，项目不一定能编译通过，仅供大家参考以及了解项目进度

# 开发计划

1. 客户端与服务端代码重构，合并在同一sdk中，修改内部设计。
2. 增加管道概念，多个外部网络连接允许使用同一管道进行通讯，避免多次打洞造成失败率提升的问题。
3. 增账号功能，允许获取同一账号下的设备列表。
4. 完善授权功能。
5. 完善安全检测，增加蜜罐端口、ip限制、双端口辅助连接等。
6. 增加客户端的wpf运行界面
7. 增加打洞Nat检测、延迟校准等能力，提升tcp打洞成功率
8. 增加quick以及kcp协议支持（客户端-客户端，客户端-服务端）
9. 增加udp打洞的实现
