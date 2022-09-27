using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQDistributedMonitor
{
    [MessagePackObject]
    public class Token<T>
    {
        public Token(List<int> queue, int[] rN, int to, T obj)
        {
            Queue = queue;
            RN = rN;
            To = to;
            Obj = obj;
        }

        [Key(0)]
        public List<int> Queue { get; set; }
        [Key(1)]
        public int[] RN { get; set; }
        [Key(2)]
        public int To { get; set; }
        [Key(3)]
        public T Obj { get; set; }
    }
}
