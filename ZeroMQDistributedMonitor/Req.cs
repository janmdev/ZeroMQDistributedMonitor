using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQDistributedMonitor
{
    [MessagePackObject]
    public class Req
    {
        public Req(int sender, int sN)
        {
            Sender = sender;
            SN = sN;
        }

        [Key(0)]
        public int Sender { get; set; }
        [Key(1)]
        public int SN { get; set; }
    }
}
