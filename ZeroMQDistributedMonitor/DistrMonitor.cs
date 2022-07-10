using NetMQ;
using NetMQ.Sockets;
using MessagePack;

namespace ZeroMQDistributedMonitor
{
    public class DistrMonitor<T> : IDisposable
    {
        private readonly string _objTopic = "obj";
        private readonly string _lockTopic = "lock";
        private readonly string _syncTopic = "sync";
        public DistrMonitor(T obj, string pubAddress, IEnumerable<string> subAddresses)
        {
            locked = false;
            if(obj == null) throw new ArgumentNullException(nameof(obj));
            distributedObject = obj;
            this.address = subAddresses;
            pubSocket = new PublisherSocket();

            pubSocket.Bind($"tcp://{pubAddress}");
            
            subSocket = new SubscriberSocket();
            foreach(var item in subAddresses)
            {
                subSocket.Connect($"tcp://{item}");
                subSocket.Subscribe(_objTopic);
                subSocket.Subscribe(_lockTopic);
                subSocket.Subscribe(_syncTopic);
            }
            Task.Run(receiveMessages);
        }

        private bool locked;
        private bool processing;
        private T distributedObject;
        private int index;
        private IEnumerable<string> address;

        private PublisherSocket pubSocket;
        private SubscriberSocket subSocket;

        public void Execute(Func<T,T> func)
        {
            lock(this)
            {
                while (locked) Monitor.Wait(this);
                sendLock();
                var res = func.Invoke(distributedObject);
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
            //pubSocket.Send()
        }

        private void sendSyncObj()
        {
            var serialized = MessagePackSerializer.Serialize(typeof(T), distributedObject);
            Msg msg = new Msg();
            msg.InitPool(serialized.Length);
            msg.Put(serialized, 0, serialized.Length);
            pubSocket.SendMoreFrame(_syncTopic).Send(ref msg, false);
            //pubSocket.Send()
        }

        private void sendLock()
        {
            processing = true;
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
            processing = false;
        }

        private async Task receiveMessages()
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
                    if(processing == true)
                    {

                    }
                    T receivedDeserialized = MessagePackSerializer.Deserialize<T>(receivedObj);
                    distributedObject = receivedDeserialized;
                }
                if(topic == _syncTopic)
                {

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