using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Instasamka247.Internet;

public class MyClient
{
    const int bufferSizeLimit = 1000 * 100;

    readonly object locker = new();
    bool writing = false;

    readonly List<byte> buffer = new(bufferSizeLimit);

    public readonly HttpListenerContext context;

    bool isClosed = false;

    public event EventHandler<Exception>? ClientClosed;

    public MyClient(HttpListenerContext context)
    {
        this.context = context;
    }

    public void WriteBytes(IEnumerable<byte> bytes)
    {
        lock (locker)
        {
            buffer.AddRange(bytes);

            if (buffer.Count > bufferSizeLimit)
            {
                System.Console.WriteLine("Оверфлоу.");
                buffer.Clear();
                return;
            }
        }

        lock (locker)
        {
            if (writing)
                return;

            writing = true;

            Task.Run(ProcessAsync);
        }
    }

    async Task ProcessAsync()
    {
        while (!isClosed)
        {
            try
            {
                byte[] smallBuffer;

                lock (locker)
                {
                    smallBuffer = buffer.Take(1000).ToArray();
                    buffer.RemoveRange(0, smallBuffer.Length);
                }

                await context.Response.OutputStream.WriteAsync(smallBuffer.AsMemory(0, smallBuffer.Length));
                await context.Response.OutputStream.FlushAsync();

                lock (locker)
                {
                    if (buffer.Count == 0)
                    {
                        writing = false;
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                isClosed = true;

                ClientClosed?.Invoke(this, e);
                return;
            }
        }
    }
}
