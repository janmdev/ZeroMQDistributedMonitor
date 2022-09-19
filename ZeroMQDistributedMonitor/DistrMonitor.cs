using NetMQ;
using NetMQ.Sockets;
using MessagePack;

namespace ZeroMQDistributedMonitor
{
    public class DistrMonitor<T> : IDisposable
    {
        private readonly string _objTopic = "obj";
        private readonly string _lockTopic = "lock";
        public DistrMonitor(string pubAddress, IEnumerable<string> subAddresses)
        {
            locked = false;
            distributedObject = default;
            pubSocket = new PublisherSocket();

            pubSocket.Bind($"tcp://{pubAddress}");
            
            subSocket = new SubscriberSocket();
            foreach(var item in subAddresses)
            {
                subSocket.Connect($"tcp://{item}");
                subSocket.Subscribe(_objTopic);
                subSocket.Subscribe(_lockTopic);
            }
            Task.Run(receiveMessages);
        }

        private bool locked;
        private T distributedObject;

        private PublisherSocket pubSocket;
        private SubscriberSocket subSocket;

        public void Execute(Func<T,T> func)
        {
            lock(this)
            {
                Console.WriteLine(locked ? "locked" : "");
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
                sendDistrObj();
                sendRelease();
                Monitor.Pulse(this);
            }
        }

        private void sendDistrObj()
        {
            var serialized = MessagePackSerializer.Serialize(typeof(T), distributedObject);
            Msg msg = new Msg();
            msg.InitPool(serialized.Length);
            msg.Put(serialized,0, serialized.Length);
            pubSocket.SendMoreFrame(_objTopic).Send(ref msg, false);
        }

        private void sendLock()
        {
            Msg msg = new Msg();
            msg.InitPool(1);
            msg.Put(new byte[] { 1 }, 0, 1);
            pubSocket.SendMoreFrame(_lockTopic).Send(ref msg, false);
        }

        private void sendRelease()
        {
            Msg msg = new Msg();
            msg.InitPool(1);
            msg.Put(new byte[] { 0 }, 0, 1);
            pubSocket.SendMoreFrame(_lockTopic).Send(ref msg, false);
        }

        private Task receiveMessages()
        {
            while(true)
            {

                string topic = subSocket.ReceiveFrameString();
                byte[] receivedObj = subSocket.ReceiveFrameBytes();
                if(topic == _lockTopic)
                {
                    if (receivedObj.First() == 0)
                    {
                        locked = false;
                        lock (this)
                            Monitor.Pulse(this);
                    }
                    if (receivedObj.First() == 1)
                    {
                        locked = true;
                    }
                }
                if(topic == _objTopic)
                {
                    distributedObject = MessagePackSerializer.Deserialize<T>(receivedObj);
                }
            }
        }

        private T lastDistrObj;

        public void Dispose()
        {
            pubSocket?.Dispose();
            subSocket?.Dispose();
        }
    }
}