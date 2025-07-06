// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");





int numTasks = 200;
int numIterations = 2000;
int maxdop = 100;
string url = "logRequest";
url = "observeScope";

var tasks = new List<Action>();
for(int i = 0; i < numTasks; i++)
{
    tasks.Add(() =>
    {
        var client = new HttpClient();
        for(int j = 0; j < numIterations; j++)
        {
            var mes = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:5247/{url}");
            var sessid = $"{Thread.CurrentThread.ManagedThreadId}-{j}";
            mes.Headers.Add("sess-id", sessid);
            var resp = client.SendAsync(mes).Result;

            if (resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var response = resp.Content.ReadAsStringAsync().Result;
                if (response != "true")
                    Console.WriteLine($"response is false for sess id {sessid}");
            }
            else {
                Console.WriteLine($"response NOK for sessId {sessid}");
            }
        }

    });
}

Console.WriteLine("Starting tasks");

Parallel.Invoke(new ParallelOptions() { MaxDegreeOfParallelism = maxdop },
    tasks.ToArray());

//Task.WaitAll(tasks.ToArray());

Console.WriteLine("the end");

