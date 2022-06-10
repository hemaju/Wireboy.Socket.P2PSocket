using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocektLib.Command
{
    public enum Role
    {
        Unknow,
        Guest,
        Member,
        Admin,
        Supadmin
    }

    public class ServerExcuteAttribute : Attribute
    {
        public RequestEnum RequestType { set; get; }
        public Role Role { set; get; }
        public ServerExcuteAttribute(RequestEnum type, Role role = Role.Unknow)
        {
            RequestType = type;
            Role = role;
        }
    }
}
