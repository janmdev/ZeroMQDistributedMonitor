using NetMQ;
using NetMQ.Sockets;
using MessagePack;
using System.Collections.Generic;

namespace ZeroMQDistributedMonitor
{
    public class DistrMonitor<T> : IDisposable
    {
        private readonly string _objTopic = "obj";
        private readonly string _lockTopic = "lock";
        public DistrMonitor(string pubAddress, IEnumerable<string> subAddresses)
        {
            locked = false;
            distributedObject = new VersionedObj<T>();
            this.address = subAddresses.Select(p => new RaftAddr(p));
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

        private int _lockedVersion;
        private int lockVersion
        {
            get
            {
                return _lockedVersion;
            }
            set
            {
                if(value >= Int32.MaxValue)
                {
                    _lockedVersion = Int32.MinValue;
                }
            }
        }
        private bool locked;
        private VersionedObj<T> distributedObject;
        private IEnumerable<RaftAddr> address;

        private PublisherSocket pubSocket;
        private SubscriberSocket subSocket;

        public void Execute(Func<T,T> func)
        {
            lock(this)
            {
                if (distributedObject is List<int> lst)
                {
                    Console.WriteLine("pre {" + String.Join(",", lst.Select(p => p.ToString()).ToArray()) + "}");
                }
                while (locked)
                    Monitor.Wait(this);
                sendLock();
                distributedObject.Value = func.Invoke(distributedObject.Value);
                distributedObject.Version++;
                sendDistrObj();
                sendRelease();
                Monitor.Pulse(this);
                if (distributedObject is List<int> lst2)
                {
                    Console.WriteLine("post {" + String.Join(",", lst2.Select(p => p.ToString()).ToArray()) + "}");
                }
            }
        }

        private void sendDistrObj()
        {
            var serialized = MessagePackSerializer.Serialize(typeof(VersionedObj<T>), distributedObject);
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
                    VersionedObj<T> receivedDeserialized = MessagePackSerializer.Deserialize<VersionedObj<T>>(receivedObj);
                    if(receivedDeserialized.Version < distributedObject.Version)
                    {
                        sendDistrObj();
                    }
                    else distributedObject = receivedDeserialized;
                }
            }
        }

        public void Dispose()
        {
            pubSocket?.Dispose();
            subSocket?.Dispose();
        }

        private class RaftAddr
        {
            public RaftAddr(string address)
            {
                Address = address;
            }

            public string Address { get; set; }
            public bool IsLeader { get; set; }
        }
    }
}