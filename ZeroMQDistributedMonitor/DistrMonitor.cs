using NetMQ;
using NetMQ.Sockets;
using MessagePack;
using System.Threading;

namespace ZeroMQDistributedMonitor
{
    public record ZmqAddr(string addr, int index);
    public class DistrMonitor<T> : IDisposable
    {
        private const string _tokenTopic = "tok";
        private const string _requestTopic = "req";

        private ZmqAddr thisAddr;
        private Token<T> token;
        private ZmqAddr[] addresses;
        private int[] rn;
        private NetMQPoller poller;
        private PublisherSocket pubSocket;
        private SubscriberSocket subSocket;
        private bool tokenReady;
        private List<Req> localQ;

        public DistrMonitor(ZmqAddr pubAddress, IEnumerable<ZmqAddr> subAddresses, bool initLock = true, int startMs = 1000)
        {
            localQ = new List<Req>();
            tokenReady = false;
            pubSocket = new PublisherSocket();
            pubSocket.Bind($"tcp://{pubAddress.addr}");
            thisAddr = pubAddress;
            addresses = subAddresses.Append(pubAddress).OrderBy(p => p.index).ToArray();
            if (addresses.GroupBy(p => p.index).Any(p => p.Count() > 1)) throw new Exception("Index duplicate");
            subSocket = new SubscriberSocket();
            foreach(var item in subAddresses)
            {
                subSocket.Connect($"tcp://{item.addr}");
                subSocket.Subscribe(_tokenTopic);
                subSocket.Subscribe(_requestTopic);
            }
            Thread.Sleep(startMs);
            rn = new int[addresses.Max(p => p.index + 1)];
            if (!initLock)
            {
                token = new Token<T>(new List<int>(), new int[rn.Length], -1, default);
            }
            else token = null;

            poller = new NetMQPoller { subSocket };
            subSocket.ReceiveReady += receiveMessages;
            poller.RunAsync();
            //Task.Run(receiveMessages);
        }
        public void Execute(Func<T,T> func)
        {
            lock(this)
            {
                if (token == null)
                {
                    rn[thisAddr.index]++;
                    sendReq();
                }
                while(token == null)
                    Monitor.Wait(this);
                tokenReady = false;
                token.Obj = func.Invoke(token.Obj);
#if DEBUG
                if (token.Obj is List<int> lst2)
                {
                    Console.WriteLine(thisAddr.index + " {" + String.Join(",", lst2.Select(p => p.ToString()).ToArray()) + "}");
                }
#endif
                if (token.Queue.Count > 0)
                {
                    sendToken();
                }
                else
                {
                    tokenReady = true;
                }
            }
        }

        private void sendReq()
        {
            var serialized = MessagePackSerializer.Serialize(typeof(Req), new Req(thisAddr.index, rn[thisAddr.index]));
            Msg msg = new Msg();
            msg.InitPool(serialized.Length);
            msg.Put(serialized, 0, serialized.Length);
            pubSocket.SendMoreFrame(_requestTopic).Send(ref msg, false);
        }

        private void sendToken()
        {
            if(token.Queue.Count > 1)
            {

            }
            token.To = token.Queue.First();
            token.Queue.RemoveAt(0);
            var serialized = MessagePackSerializer.Serialize(typeof(Token<T>), token);
            Msg msg = new Msg();
            msg.InitPool(serialized.Length);
            msg.Put(serialized, 0, serialized.Length);
            pubSocket.SendMoreFrame(_tokenTopic).Send(ref msg, false);
            token = null;
        }

        private void receiveMessages(object? sender, NetMQSocketEventArgs args)
        {
            string topic = args.Socket.ReceiveFrameString();
            byte[] receivedObj = args.Socket.ReceiveFrameBytes();
            switch (topic)
            {
                case _tokenTopic:
                    var lockObj = MessagePackSerializer.Deserialize<Token<T>>(receivedObj);
                    processToken(lockObj);
                    break;
                case _requestTopic:
                    var req = MessagePackSerializer.Deserialize<Req>(receivedObj);
                    processReq(req);
                    break;
            }
        }

        private void processToken(Token<T> tok)
        {
            lock (this)
            {
                if (tok.To == thisAddr.index)
                {
                    token = tok;
                    foreach(var localReq in localQ)
                    {
                        if(!token.Queue.Contains(localReq.Sender) && token.RN[localReq.Sender] < localReq.SN && localReq.Sender != thisAddr.index)
                        {
                            token.Queue.Add(localReq.Sender);
                        }
                    }
                    localQ.Clear();
                    Monitor.Pulse(this);
                }
            }
        }

        private void processReq(Req req)
        {
            lock (this)
            {
                rn[req.Sender] = Math.Max(rn[req.Sender], req.SN);
                if (!token?.Queue.Contains(req.Sender) ?? false)
                {
                    token.Queue.Add(req.Sender);
                    if (tokenReady)
                    {
                        tokenReady = false;
                        sendToken();
                    }
                } else
                {
                    localQ.Add(req);
                }
            }
        }

        public void Dispose()
        {
            poller?.Dispose();
            pubSocket?.Dispose();
            subSocket?.Dispose();
        }
    }
}