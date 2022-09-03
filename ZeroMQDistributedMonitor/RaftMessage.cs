using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQDistributedMonitor
{
    public class RaftMessage
    {
        public MessageType Type { get; set; }
        
    }
    public enum MessageType
    {
        HeartBeat,
        VoteYes,
        VoteNo,
        Elect
    }
}
