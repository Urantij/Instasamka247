using System;
using System.Net;
using Instasamka247.Internet;

namespace Instasamka247;

class Program
{
    const int bufferSize = 1024;

    public const string songsFolderPath = "./Songs";

    static async Task Main(string[] appArgs)
    {
        Console.WriteLine("Hello, World!");

        string serverAddress = appArgs[0];

        TaskScheduler.UnobservedTaskException += (sender, eArgs) =>
        {
            System.Console.WriteLine($"UnobservedTaskException\n{eArgs.Exception}");
        };

        if (!Directory.Exists(songsFolderPath))
        {
            Directory.CreateDirectory(songsFolderPath);
        }

        string[] songsFiles = Directory.GetFiles(songsFolderPath, "*.*");

        if (songsFiles.Length == 0)
        {
            System.Console.WriteLine("А песен то нет.");
            return;
        }

        CancellationTokenSource cts = new();

        Ffmpeger ffmpeger = Ffmpeger.Create("ffmpeg");

        GrandServer server = new(serverAddress);

        Task readTextLoopTask = Task.Run(() => ReadTextLoopAsync(ffmpeger, cts.Token));
        Task readLoopTask = Task.Run(() => ReadLoopAsync(server, ffmpeger, cts.Token));
        Task writeLoopTask = Task.Run(() => WriteLoopAsync(songsFiles, ffmpeger, cts.Token));

        server.Start();

        while (true)
        {
            System.Console.WriteLine("хватит");
            string? input = System.Console.ReadLine();

            if (input == null)
                continue;

            if (input.Equals("хватит", StringComparison.OrdinalIgnoreCase))
            {
                server.Stop();
                await ffmpeger.InputStream.DisposeAsync();

                cts.Cancel();

                await readTextLoopTask;
                await readLoopTask;
                await writeLoopTask;

                cts.Dispose();
                return;
            }
        }
    }

    static async Task ReadTextLoopAsync(Ffmpeger ffmpeger, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[bufferSize];

        while (!cancellationToken.IsCancellationRequested)
        {
            int read;
            try
            {
                read = await ffmpeger.TextStream.BaseStream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            if (read == 0)
                break;

            // string text = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
            // System.Console.WriteLine(text);
        }

        System.Console.WriteLine("Закончили чтение текста ффмпега.");
    }

    static async Task ReadLoopAsync(GrandServer server, Ffmpeger ffmpeger, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[bufferSize];

        while (!cancellationToken.IsCancellationRequested)
        {
            int read;
            try
            {
                read = await ffmpeger.OutputStream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            if (read == 0)
                break;

            MyClient[] clients = server.GetClients();

            byte[] effectiveBuffer = buffer[0..read];

            foreach (var client in clients)
            {
                client.WriteBytes(effectiveBuffer);
            }
        }

        System.Console.WriteLine("Чтение ффмпега закончили.");
    }

    static async Task WriteLoopAsync(string[] pathes, Ffmpeger ffmpeger, CancellationToken cancellationToken)
    {
        int current = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            string path = pathes[current];

            current++;
            if (current >= pathes.Length)
                current = 0;

            using FileStream fs = new(path, FileMode.Open, FileAccess.Read);

            try
            {
                await fs.CopyToAsync(ffmpeger.InputStream, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        System.Console.WriteLine("Запись в ффмпег закончили.");
    }
}
