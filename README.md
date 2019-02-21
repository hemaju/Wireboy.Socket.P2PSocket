# Wireboy.Socket.P2PSocket

喜欢此项目的，请点击一下右上角的Star

加入QQ群417159195，与作者共同交流，入群途径请填写“P2PSocket”

## 开发计划（开发顺序不分先后）

1.客户端与服务端均增加日志功能

2.客户端（主控端）增加断线重连功能

3.为客户端与服务端增加配置文件

4.TCP点对点连接（不经过服务器）

5.客户端（被控端）增加密码验证

6.服务端增加用户名与密码验证（数据库）

## 当前项目状态？

1.master分支版本已稳定，但需要修改服务端ip编译后使用。

2.dev分支版本为开发者活跃版本，新功能会先加入dev分支，稳定后与master分支合并。

## 网络结构

![img4](Images/img4.png)

## 先来个项目介绍吧

此项目在正确设置服务端ip后，可用于mstsc进行远程桌面控制（我的目的也如此）。

这是一个使用.NetCore控制台项目作为服务端，.netframework4.5的C#控制台项目作为主控与被控客户端的项目。

结论：这是一个假的p2p服务

## 如何使用？

编译环境：VS2017 + .Net Framework 4.5  + .Net Core 2.1

1.修改项目Wireboy.Socket.P2PHome与项目Wireboy.Socket.P2PClient的服务器ip地址（service_IpAddress变量）。

![img1](Images/img1.png)

2.编译项目Wireboy.Socket.P2PService（服务端）、Wireboy.Socket.P2PHome（被控客户端）、Wireboy.Socket.P2PClient（主控客户端）

3.将服务端P2PService.dll部署到拥有公网ip的服务器，并运行

4.将主控端与被控端在两台不同的机器上运行，输入服务器名称（名称任意，仅用于主控与被控进行匹配）。

5.打开主控端电脑的mstsc，使用ip：127.0.0.1:3388连接被控客户端电脑即可。

注：被控端电脑需要开启远程服务，如下图：

![img2](Images/img2.png)

## 运行效果图

![img3](Images/img3.gif)

## 更新日志

### 2019年2月20日

1.解决第二次连接失败的问题

2.增加被控端（Home）的日志记录功能


