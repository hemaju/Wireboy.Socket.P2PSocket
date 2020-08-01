using System;
using System.Collections.Generic;
using System.Text;

namespace P2PSocket.Core.Utils
{
    public static class EasyOp
    {
        public static bool Do(Action func, Action<Exception> errorHandle)
        {
            bool ret = true;
            try
            {
                func();
            }
            catch (Exception ex)
            {
                ret = false;
                errorHandle(ex);
            }
            return ret;
        }
        public static void Do(Action func, Action successHandle, Action<Exception> errorHandle)
        {
            bool ret = true;
            try
            {
                func();
            }
            catch (Exception ex)
            {
                ret = false;
                errorHandle(ex);
            }
            if (ret) successHandle();
        }
        public static bool Do(Action func)
        {
            bool ret = false;
            try
            {
                func();
                ret = true;
            }
            catch { }
            return ret;
        }
    }
}
