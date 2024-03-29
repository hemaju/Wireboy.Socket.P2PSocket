﻿using P2PSocektLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace P2PSocektLib.Command
{
    internal class Request_C_Service : BaseService
    {
        #region 客户端 有返回
        /// <summary>
        /// 发送心跳包
        /// </summary>
        /// <returns></returns>
        public async Task Heart(Func<byte[], Task> sendFunc)
        {
            byte[] data = new byte[] { 99 };
            byte[] packData = CmdPacket.PackOne(data, 0, RequestEnum.客户端认证);
            await sendFunc(packData);
        }
        /// <summary>
        /// 登录（匿名）
        /// </summary>
        /// <param name="sendFunc">[方法]发送数据</param>
        /// <param name="model">登录的数据</param>
        /// <returns></returns>
        public async Task<ApiModel_Login_R?> Login(Func<byte[], int, Task> sendFunc, ApiModel_Login model)
        {
            byte[] data = ModelToBytes(model);
            uint token = GetNewToken();
            byte[] packData = CmdPacket.PackOne(data, token, RequestEnum.客户端认证);
            byte[] res = await TaskUtil.Wait(token, () => sendFunc(packData, packData.Length));
            return BytesToModel<ApiModel_Login_R>(res);
        }
        #endregion

        #region 客户端 无返回
        public async Task CreatePipe(Func<byte[], int, Task> sendFunc, ApiModel_CreatePipe model)
        {
            byte[] data = ModelToBytes(model);
            byte[] packData = CmdPacket.PackOne(data, 0, RequestEnum.管道_管道建立);
            await sendFunc(packData, packData.Length);
        }
        #endregion

    }
}
