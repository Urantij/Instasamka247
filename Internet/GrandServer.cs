using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Instasamka247.Internet;

public class GrandServer
{
    readonly HttpListener listener;

    readonly List<MyClient> clients = new();

    public GrandServer(string address)
    {
        listener = new();
        listener.Prefixes.Add($"http://{address}:1488/music.audio/");
    }

    public void Start()
    {
        listener.Start();

        Task.Run(LoopAsync);
    }

    public void Stop()
    {
        listener.Stop();

        MyClient[] sadClients;
        lock (clients)
        {
            sadClients = clients.ToArray();
            clients.Clear();
        }

        if (sadClients.Length == 0)
            return;

        foreach (MyClient client in sadClients)
        {
            client.context.Response.Abort();
        }
    }

    public MyClient[] GetClients()
    {
        lock (clients)
        {
            return clients.ToArray();
        }
    }

    async Task LoopAsync()
    {
        while (listener.IsListening)
        {
            HttpListenerContext context = await listener.GetContextAsync();

            System.Console.WriteLine("Коннектед.");

            context.Response.AppendHeader("Content-Type", "audio/mpeg");
            // context.Response.AppendHeader("Content-Type", "audio/aac");

            MyClient client = new(context);

            lock (clients)
            {
                clients.Add(client);
                client.ClientClosed += ClientClosed;
            }
        }
    }

    private void ClientClosed(object? sender, Exception e)
    {
        System.Console.WriteLine("Дисконнектед.");

        MyClient client = (MyClient)sender!;

        lock (clients)
        {
            clients.Remove(client);
            client.ClientClosed -= ClientClosed;
        }
    }
}
