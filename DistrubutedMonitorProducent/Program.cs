using ZeroMQDistributedMonitor;

Console.WriteLine("Producer");

using (var monitor = new DistrMonitor<List<int>>(new List<int>(), "127.0.0.1:6665", new string[] { "127.0.0.1:6664" }))
{
    int counter = 0;
    while (true)
    {
        monitor.Execute(list =>
        {
            if (list.Count <= 20)
            {
                list.Add(counter++);
                Console.WriteLine("{" + String.Join(",",list.Select(p => p.ToString()).ToArray()) + "}");
                //Thread.Sleep(100);
            }
            return list;
        });
        Thread.Sleep(20);
    }

}
