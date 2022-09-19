using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQDistributedMonitor
{
    [MessagePackObject]
    public class VersionedObj<T>
    {
        [Key(0)]
        public int Version;
        [Key(1)]
        public T Value;

        public VersionedObj()
        {
            Value = default;
        }
    }
}
