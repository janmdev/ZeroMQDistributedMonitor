using ZeroMQDistributedMonitor;

Console.WriteLine("Producer");

using (var monitor = new DistrMonitor<List<int>>(new List<int>(), "127.0.0.1:6665", new string[] { "127.0.0.1:6664" }))
{
    while (true)
    {
        monitor.Execute(list =>
        {
            if (list.Count <= 20)
            {
                list.Add(5);
                Console.WriteLine(list.Count);
                Thread.Sleep(200);
            }
            return list;
        });
    }

}
