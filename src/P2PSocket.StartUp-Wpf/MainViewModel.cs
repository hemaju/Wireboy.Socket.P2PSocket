using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace P2PSocket.StartUp_Wpf
{
    public class MainViewModel : PropertyStore
    {
        public string LogMessage
        {
            get
            {
                return Get<string>();
            }
            set
            {
                Set(value);
            }
        }
        public string ServerAddress
        {
            get
            {
                return Get<string>();
            }
            set
            {
                Set(value);
            }
        }
        public int TcpCount
        {
            get
            {
                return Get<int>();
            }
            set
            {
                Set(value);
            }
        }

    }
    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        protected void DoPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected void DoAllPropertyChanged()
        {
            DoPropertyChanged(null);
        }
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            DoPropertyChanged(propertyName);
        }
    }
    public class PropertyStore : NotifyPropertyChanged
    {
        readonly Dictionary<string, object> _store = new Dictionary<string, object>();
        protected T Get<T>(T Default = default(T), [CallerMemberName] string propertyName = "")
        {
            lock (_store)
            {
                if (_store.TryGetValue(propertyName, out var obj) && obj is T val)
                {
                    return val;
                }
            }
            return Default;
        }

        protected void Set<T>(T Value, [CallerMemberName] string propertyName = "")
        {
            lock (_store)
            {
                if (_store.ContainsKey(propertyName))
                {
                    _store[propertyName] = Value;
                }
                else
                    _store.Add(propertyName, Value);
            }
            OnPropertyChanged(propertyName);
        }
    }
}
