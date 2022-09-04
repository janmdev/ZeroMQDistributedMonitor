using ZeroMQDistributedMonitor;

Console.WriteLine("Producer");

Task.Run(() =>
{
    using (var monitor = new DistrMonitor<List<int>>("127.0.0.1:6665", new string[] { "127.0.0.1:6664", "127.0.0.1:6666" }))
    {
        int counter = 0;
        while (true)
        {
            monitor.Execute(list =>
            {
                if (list == default) list = new List<int>();
                if (list.Count <= 20)
                {
                    list.Add(counter++);
                    Console.WriteLine("P1: {" + String.Join(",", list.Select(p => p.ToString()).ToArray()) + "}");
                }
                return list;
            });
            Thread.Sleep(100);
        }

    }
});

Console.WriteLine("Wait for P2");
Thread.Sleep(5000);
Console.WriteLine("P2 Start");
Task.Run(() =>
{
    using (var monitor = new DistrMonitor<List<int>>("127.0.0.1:6666", new string[] { "127.0.0.1:6664", "127.0.0.1:6665" }))
    {
        int counter = 0;
        while (true)
        {
            monitor.Execute(list =>
            {
                if (list == default) list = new List<int>();
                if (list.Count <= 20)
                {
                    counter += 10;
                    list.Add(counter);
                    Console.WriteLine("P2: {" + String.Join(",", list.Select(p => p.ToString()).ToArray()) + "}");
                }
                return list;
            });
            Thread.Sleep(100);
        }

    }
});

while (true)
{

}

