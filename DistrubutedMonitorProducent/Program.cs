using ZeroMQDistributedMonitor;

Console.WriteLine("Producer");
Task.Run(() =>
{
    using (var monitor = new DistrMonitor<List<int>>(new("127.0.0.1:6665",1), new ZmqAddr[] { new("127.0.0.1:6664",0), new("127.0.0.1:6666",2) }))
    {
        int counter = 0;
        while (true)
        {
            monitor.Execute(list =>
            {
                Thread.Sleep(200);
                if (list == default) list = new List<int>();
                if (list.Count <= 20)
                {
                    list.Add(counter++);
                    //Console.WriteLine("P1: {" + String.Join(",", list.Select(p => p.ToString()).ToArray()) + "}");
                }
                return list;
            });
        }

    }
});

//Console.WriteLine("Wait for P2");
Console.WriteLine("P2 Start");
Task.Run(() =>
{
    using (var monitor = new DistrMonitor<List<int>>(new("127.0.0.1:6666",2), new ZmqAddr[] { new("127.0.0.1:6664",0), new("127.0.0.1:6665",1) }))
    {
        int counter = 0;
        while (true)
        {
            monitor.Execute(list =>
            {
                Thread.Sleep(200);
                if (list == default) list = new List<int>();
                if (list.Count <= 20)
                {
                    counter += 10;
                    list.Add(counter);
                    //Console.WriteLine("P2: {" + String.Join(",", list.Select(p => p.ToString()).ToArray()) + "}");
                }
                return list;
            });
        }

    }
});

while (true)
{

}

