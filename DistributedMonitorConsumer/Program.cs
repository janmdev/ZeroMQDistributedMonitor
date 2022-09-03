using ZeroMQDistributedMonitor;

Console.WriteLine("Consumer");

using (var monitor = new DistrMonitor<List<int>>(new List<int>(), "127.0.0.1:6664", new string[] { "127.0.0.1:6665" }))
{
    while (true)
    {
        monitor.Execute(list =>
        {
            Console.WriteLine("pre {" + String.Join(",", list.Select(p => p.ToString()).ToArray()) + "}");
            if (list.Count > 0)
            {
                list.RemoveAt(0);
                Console.WriteLine("post {" + String.Join(",", list.Select(p => p.ToString()).ToArray()) + "}");
            
            }
            return list;
        });
        Thread.Sleep(50);
    }
}
