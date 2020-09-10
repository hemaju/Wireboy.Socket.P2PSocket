using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace P2PSocket.Core.Utils
{
    public interface IEasyInjectObject
    {
        /// <summary>
        /// 注册为通用模式
        /// </summary>
        /// <param name="canOverride"></param>
        void Common(bool canOverride = false);
        /// <summary>
        /// 注册为继承模式
        /// </summary>
        /// <param name="canOverride"></param>
        void Inherited(bool canOverride = false);
        /// <summary>
        /// 注册为单例模式
        /// </summary>
        /// <param name="canOverride"></param>
        void Singleton(bool canOverride = false);
    }
    public class EasyInject : IEasyInjectObject
    {
        enum ImpType
        {
            Common, //普通模式
            Inherited,    //继承模式
            Singleton   //单例模式
        }
        static Dictionary<Type, (ImpType, EasyInject, EasyInject)> ImplDic = new Dictionary<Type, (ImpType, EasyInject, EasyInject)>();
        static Dictionary<Type, List<Type>> ImplArraryDic = new Dictionary<Type, List<Type>>();
        public static void ClearAll()
        {
            ImplDic.Clear();
            ImplArraryDic.Clear();
        }

        /// <summary>
        /// 注册一个接口
        /// </summary>
        /// <typeparam name="T1">接口类</typeparam>
        /// <typeparam name="T2">实现类</typeparam>
        /// <returns></returns>
        public static IEasyInjectObject Put<T1, T2>() where T1 : class where T2 : T1
        {
            return new EasyInject(typeof(T1), typeof(T2));
        }

        public static void Push<T1, T2>(Func<Type, bool> func) where T1 : class where T2 : T1
        {
            Type key = typeof(T1);
            if (ImplArraryDic.ContainsKey(key))
            {
                ImplArraryDic[key] = ImplArraryDic[key].Where(t => !func(t)).ToList();
                ImplArraryDic[key].Add(typeof(T2));
            }
            else
            {
                ImplArraryDic.Add(key, new List<Type>() { typeof(T2) });
            }
        }

        public static T Get<T>(Func<Type, bool> func) where T : class
        {
            Type key = typeof(T);
            if (ImplArraryDic.ContainsKey(key))
            {
                IEnumerable<Type> list = ImplArraryDic[key].Where(t => func(t));
                if (list.Count() > 0)
                {
                    if (list.Count() > 1) throw new ArgumentException("无法确认唯一值，查询到多条数据");
                    return Activator.CreateInstance(list.First()) as T;
                }
            }
            return default;
        }

        public static T Get<T>(bool isParent = false) where T : class
        {
            Type key = typeof(T);
            if (ImplDic.ContainsKey(key))
            {
                if (ImplDic[key].Item1 == ImpType.Singleton)
                    return ImplDic[key].Item2.GetSingletonInstance() as T;
                if (ImplDic[key].Item1 == ImpType.Inherited)
                {
                    if (isParent)
                    {
                        if (ImplDic[key].Item3 == null)
                            return null;
                        return ImplDic[key].Item3.CreateInstance() as T;
                    }
                    else
                        return ImplDic[key].Item2.CreateInstance() as T;
                }
                else
                    return ImplDic[key].Item2.CreateInstance() as T;
            }
            else
                return default;
        }
        Type iface;
        Type impl;
        EasyInject(Type iface, Type impl)
        {
            this.iface = iface;
            this.impl = impl;
        }

        /// <summary>
        ///  普通模式（重复注册会报错）
        /// </summary>
        public void Common(bool canOverride = false)
        {
            if (!Bind(value =>
            {
                if (canOverride)
                {
                    value.Item2 = this;
                    ImplDic[iface] = value;
                }
                else
                    throw new InvalidOperationException($"重复注册接口：{iface}");
            }))
            {
                ImplDic.Add(iface, (ImpType.Common, this, null));
            }
        }

        /// <summary>
        /// 继承模式（允许同一接口注册2个实例）
        /// </summary>
        public void Inherited(bool canOverride = false)
        {

            if (!Bind(value =>
            {
                if (value.Item1 == ImpType.Singleton)
                    throw new InvalidOperationException($"接口{iface}已被注册为{value.Item1}模式");
                //转换接口注册类型
                if (value.Item1 == ImpType.Common)
                    value.Item1 = ImpType.Inherited;
                if (value.Item2 == null)
                    value.Item2 = this;
                else if (value.Item3 == null)
                {
                    value.Item3 = value.Item2;
                    value.Item2 = this;
                }
                else if (canOverride)
                    value.Item2 = this;
                else
                    throw new InvalidOperationException($"重复注册接口：{iface} 仅允许注册一个子级");
                ImplDic[iface] = value;
            }))
            {
                ImplDic.Add(iface, (ImpType.Inherited, this, null));
            }
        }

        /// <summary>
        /// 单例模式
        /// </summary>
        public void Singleton(bool canOverride = false)
        {
            if (!Bind(value =>
            {
                if (canOverride)
                {
                    value.Item2 = this;
                    ImplDic[iface] = value;
                }
                else
                    throw new InvalidOperationException($"重复注册接口：{iface}");
            }))
            {
                ImplDic.Add(iface, (ImpType.Singleton, this, null));
            }
        }

        public object CreateInstance()
        {
            return Activator.CreateInstance(impl);
        }

        object singletonInstance = null;
        public object GetSingletonInstance()
        {
            if (singletonInstance == null)
                singletonInstance = CreateInstance();
            return singletonInstance;
        }

        bool Bind(Action<(ImpType, EasyInject, EasyInject)> func)
        {
            if (ImplDic.ContainsKey(iface))
            {
                func(ImplDic[iface]);
                return true;
            }
            return false;
        }
    }
}
