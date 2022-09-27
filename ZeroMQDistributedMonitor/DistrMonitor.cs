using NetMQ;
using NetMQ.Sockets;
using MessagePack;
using System.Threading;

namespace ZeroMQDistributedMonitor
{
    public class DistrMonitor<T> : IDisposable
    {
        private readonly string _lockTopic = "lock";
        public DistrMonitor(string pubAddress, IEnumerable<string> subAddresses, bool initLock = true)
        {
            locked = initLock;
            distributedObject = default;
            pubSocket = new PublisherSocket();
            pubSocket.Bind($"tcp://{pubAddress}");
            
            subSocket = new SubscriberSocket();
            foreach(var item in subAddresses)
            {
                subSocket.Connect($"tcp://{item}");
                subSocket.Subscribe(_lockTopic);
            }
            poller = new NetMQPoller { subSocket };
            subSocket.ReceiveReady += receiveMessages;
            poller.RunAsync();
        }

        private bool locked;
        private bool interLocked;
        private T distributedObject { get; set; }
        private NetMQPoller poller;
        private PublisherSocket pubSocket;
        private SubscriberSocket subSocket;

        public void Execute(Func<T,T> func)
        {
            lock(this)
            {
                //Console.WriteLine(locked ? "locked" : "");
                while (locked) 
                    Monitor.Wait(this);
                sendLock();
                if (distributedObject is List<int> lst)
                {
                    Console.WriteLine("pre {" + String.Join(",", lst.Select(p => p.ToString()).ToArray()) + "}");
                }
                distributedObject = func.Invoke(distributedObject);
                if (distributedObject is List<int> lst2)
                {
                    Console.WriteLine("post {" + String.Join(",", lst2.Select(p => p.ToString()).ToArray()) + "}");
                }
                //sendDistrObj();
                sendRelease(distributedObject);
                Monitor.Pulse(this);
            }
        }

        /*private void sendDistrObj()
        {
            var serialized = MessagePackSerializer.Serialize(typeof(T), distributedObject);
            Msg msg = new Msg();
            msg.InitPool(serialized.Length);
            msg.Put(serialized,0, serialized.Length);
            pubSocket.SendMoreFrame(_objTopic).Send(ref msg, false);
        }*/

        private void sendLock()
        {
            var serialized = MessagePackSerializer.Serialize(typeof(Lock<T>), new Lock<T>(true));
            Msg msg = new Msg();
            msg.InitPool(serialized.Length);
            msg.Put(serialized, 0, serialized.Length);
            pubSocket.SendMoreFrame(_lockTopic).Send(ref msg, false);
            interLocked = true;
        }

        private void sendRelease(T obj)
        {
            var serialized = MessagePackSerializer.Serialize(typeof(Lock<T>), new Lock<T>(false, obj));
            Msg msg = new Msg();
            msg.InitPool(serialized.Length);
            msg.Put(serialized, 0, serialized.Length);
            pubSocket.SendMoreFrame(_lockTopic).Send(ref msg, false);
            interLocked = false;
        }

        private void receiveMessages(object? sender, NetMQSocketEventArgs args)
        {
            string topic = args.Socket.ReceiveFrameString();
            lock(this)
            {
                if (topic == _lockTopic)
                {
                    byte[] receivedObj = args.Socket.ReceiveFrameBytes();
                    var lockObj = MessagePackSerializer.Deserialize<Lock<T>>(receivedObj);
                    if (!lockObj.IsLocked)
                    {
                        locked = false;
                        distributedObject = lockObj.Value;
                        Monitor.Pulse(this);
                        //Console.WriteLine("unlock");
                    }
                    if (lockObj.IsLocked)
                    {
                        locked = true;
                        //Console.WriteLine("lock");
                    }
                }
            }
        }

        public void Dispose()
        {
            pubSocket?.Dispose();
            subSocket?.Dispose();
        }
    }
}