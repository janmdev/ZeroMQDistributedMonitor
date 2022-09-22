using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQDistributedMonitor
{
    [MessagePackObject]
    public class Lock<T>
    {
        [Key(0)]
        public bool IsLocked { get; set; }
        [Key(1)]
        public T? Value { get; set; }
        public Lock(bool isLocked)
        {
            IsLocked = isLocked;
        }
        public Lock(bool isLocked, T val)
        {
            IsLocked = isLocked;
            Value = val;
        }
    }
}
