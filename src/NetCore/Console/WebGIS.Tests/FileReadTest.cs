using System;
using System.IO;
using System.Threading;

namespace WebGIS.Tests;

internal class FileReadTest
{
    public void Run(int max = 10)
    {
        for (int i = 0; i < max; i++)
        {
            var thread = new Thread(new ThreadStart(ReadFile));
            thread.Start();
        }
    }

    public void ReadFile()
    {
        FileInfo fi = new FileInfo("C:\\temp\\webgis5_db\\cache\\hmac~6a49fd85eb0a4621bc293db84c991d32.0.json");
        if (!fi.Exists)
        {
            throw new Exception("Not exists");
        }

        string jsonString = File.ReadAllText(fi.FullName);

        Console.WriteLine(jsonString);
    }
}
