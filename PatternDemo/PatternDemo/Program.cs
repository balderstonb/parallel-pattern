using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PatternDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var input = Enumerable.Range(1, 64).Select(i => $"https://jsonplaceholder.typicode.com/todos/{i}").ToList();
            var output = new ConcurrentBag<string>();

            var client = new HttpClient();

            var stopwatch = Stopwatch.StartNew();
            Request(input, new object[] { client, output });
            stopwatch.Stop();

            var serialOutput = new ConcurrentBag<string>(output);

            Console.WriteLine($"Serial: {stopwatch.Elapsed}");

            output = new ConcurrentBag<string>();

            stopwatch = Stopwatch.StartNew();
            RunParallel(input, Request, client, output);
            stopwatch.Stop();

            var parallelOuput = new ConcurrentBag<string>(output);

            Console.WriteLine($"Parallel: {stopwatch.Elapsed}");

            Console.WriteLine(serialOutput.OrderBy(f => f).SequenceEqual(parallelOuput.OrderBy(f => f)));
        }

        private static void Request(List<string> uris, object[] args)
        {
            var client = (HttpClient)args[0];
            var output = (ConcurrentBag<string>)args[1];

            foreach (string uri in uris)
            {
                output.Add(client.GetStringAsync(uri).Result);
            }
        }

        private static void RunParallel<T>(List<T> workToDo, Action<List<T>, object[]> worker, params object[] args)
        {
            int numberOfThreads = Environment.ProcessorCount;

            Task[] taskArray = new Task[numberOfThreads];

            for (int t = 0; t < numberOfThreads; t++)
            {
                var rowsToWorkOn = new List<T>();

                for (int s = t; s < workToDo.Count; s += numberOfThreads)
                {
                    rowsToWorkOn.Add(workToDo[s]);
                }

                taskArray[t] = Task.Factory.StartNew(() => worker(rowsToWorkOn, args));
            }

            Task.WaitAll(taskArray);
        }
    }
}
