using ZeroMQDistributedMonitor;

Console.WriteLine("Consumer");

using (var monitor = new DistrMonitor<List<int>>(new("127.0.0.1:6664",0), new ZmqAddr[] { new("127.0.0.1:6665",1), new("127.0.0.1:6666",2) }, false))
{
    while (true)
    {
        monitor.Execute(list =>
        {
            Thread.Sleep(200);
            if (list == null) list = new List<int>();
            //Console.WriteLine("pre {" + String.Join(",", list.Select(p => p.ToString()).ToArray()) + "}");
            if (list.Count > 0)
            {
                list.RemoveAt(0);
                //Console.WriteLine("post {" + String.Join(",", list.Select(p => p.ToString()).ToArray()) + "}");

            }
            return list;
        });
    }
}
