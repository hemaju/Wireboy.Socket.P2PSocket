using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace P2PSocektLib.Command
{
    /// <summary>
    /// 登录类型
    /// </summary>
    internal enum LoginType
    {
        /// <summary>
        /// 游客登录（匿名）
        /// </summary>
        Guest,
        /// <summary>
        /// token登录
        /// </summary>
        Token,
        /// <summary>
        /// 账号登录
        /// </summary>
        Account
    }
    /// <summary>
    /// 客户端认证
    /// </summary>
    internal class ApiModel_Login
    {
        /// <summary>
        /// 登陆类型
        /// </summary>
        [JsonPropertyName("A")]
        public LoginType LoginType { set; get; } = LoginType.Guest;
        /// <summary>
        /// 账号
        /// </summary>
        [JsonPropertyName("B")]
        public string Account { set; get; } = string.Empty;
        /// <summary>
        /// 密码
        /// </summary>
        [JsonPropertyName("C")]
        public string Password { set; get; } = string.Empty;
        /// <summary>
        /// Token
        /// </summary>
        [JsonPropertyName("D")]
        public string Token { set; get; } = string.Empty;
        /// <summary>
        /// mac地址
        /// </summary>
        [JsonPropertyName("E")]
        public string Mac { set; get; } = string.Empty;
    }
    internal class ApiModel_Login_R
    {
        /// <summary>
        /// 错误消息
        /// </summary>
        [JsonPropertyName("A")]
        public string Error { set; get; } = string.Empty;
        /// <summary>
        /// 客户端唯一标识
        /// </summary>
        [JsonPropertyName("B")]
        public string ClientCode { set; get; } = string.Empty;
        /// <summary>
        /// 返回的有效token，用于token登录
        /// </summary>
        [JsonPropertyName("C")]
        public string LoginToken { set; get; } = string.Empty;
    }
}
