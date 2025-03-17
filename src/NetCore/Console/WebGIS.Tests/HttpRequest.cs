using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebGIS.Tests;

internal class HttpRequest
{
    private string _url;
    private HttpClient _http;

    public void Run(int max, string url)
    {
        _url = url;
        _http = new HttpClient();

        List<Task> tasks = new List<Task>();
        for (int i = 0; i < max; i++)
        {
            tasks.Add(RequestUrl());
        }
        Task.WaitAll(tasks.ToArray());

        Console.WriteLine("Finished");
    }

    async public Task RequestUrl()
    {
        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, _url))
        {
            var response = await _http.SendAsync(requestMessage);
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
    }
}
