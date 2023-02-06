using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Instasamka247;

public class Ffmpeger
{
    readonly Process process;

    /// <summary>
    /// Ето читаем
    /// </summary>
    public Stream OutputStream => process.StandardOutput.BaseStream;

    /// <summary>
    /// Его пишем
    /// </summary>
    public Stream InputStream => process.StandardInput.BaseStream;

    public StreamReader TextStream => process.StandardError;

    public Ffmpeger(Process process)
    {
        this.process = process;
    }

    public static Ffmpeger Create(string ffmpegPath)
    {
        Process process = new();

        process.StartInfo.FileName = ffmpegPath;

        process.StartInfo.Arguments = "-re -i pipe:0 -codec:a libmp3lame -b:a 128 -f mp3 pipe:1";
        // process.StartInfo.Arguments = "-re -i pipe:0 -codec:a aac -b:a 128k -movflags +faststart -f adts pipe:1";

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; //написано, что должно быть че то тру, а оно фолс. ну похуй, работает и ладно
        process.StartInfo.CreateNoWindow = true;
        process.Start();

        return new Ffmpeger(process);
    }
}
