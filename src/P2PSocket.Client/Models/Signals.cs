using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace P2PSocket.Client.Models
{
    public class Signals
    {
        public enum SignalType
        {
            Service_Connect_Success,
            Service_Connect_Faile,
        }

        public static Dictionary<SignalType, List<Action>> signalHandle = new Dictionary<SignalType, List<Action>>();
        public static bool Connect(SignalType signalType, Action action)
        {
            if (signalHandle.ContainsKey(signalType))
            {
                signalHandle[signalType].Add(action);
            }
            else
            {
                signalHandle.Add(signalType, new List<Action>() { action });
            }
            return true;
        }

        public static void Emit(SignalType signalType)
        {
            if (signalHandle.ContainsKey(signalType))
            {
                foreach(Action action in signalHandle[signalType])
                {
                    action();
                }
            }
        }
    }
}
