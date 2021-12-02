using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public enum WriteMode
{
    Append,
    Overwrite
}

public class WriteToCSV : MonoBehaviour
{
    private string fileName;
    private string savePath;
    private string filePath;

    public LogStore LogStore;

    public WriteToCSV(LogStore logStore, string savePath, string filePrefix, string fileExtension)
    {
        this.fileName = filePrefix + "_" + logStore.Label + "_" +
                        DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ffff") + fileExtension;
        Init(logStore, savePath);
    }

    public WriteToCSV(LogStore logStore, string savePath, string fileName)
    {
        this.fileName = fileName;
        Init(logStore, savePath);
    }

    public WriteToCSV(LogStore logStore, string filePath)
    {
        this.filePath = filePath;
        this.LogStore = logStore;
    }

    private void Init(LogStore logStore, string path)
    {
       this.savePath = path == "" ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : path;
       this.filePath = Path.Combine(savePath, fileName);
       this.LogStore = logStore;
    }


    public void WriteAll()
    {
        new Thread(() =>
        {
            Stopwatch exportToStringStopwatch = new Stopwatch();
            exportToStringStopwatch.Start();

            string dataString = LogStore.ExportAll<string>();

            exportToStringStopwatch.Stop();
            TimeSpan exportToStringTs = exportToStringStopwatch.Elapsed;
            string exportToStringElapsedTime = String.Format("{0:00}:{1:0000}",
                exportToStringTs.Seconds, exportToStringTs.Milliseconds);
            Debug.Log(LogStore.Label + " string exported in " + exportToStringElapsedTime);

            string headers = LogStore.GenerateHeaders();

            Stopwatch writeStopwatch = new Stopwatch();
            writeStopwatch.Start();
            using (var file = new StreamWriter(filePath, true))
            {
                file.WriteLine(headers);
                file.Write(dataString);
            }
            writeStopwatch.Stop();
            TimeSpan writeTs = writeStopwatch.Elapsed;
            string writeElapsedTime = String.Format("{0:00}:{1:0000}",
                writeTs.Seconds, writeTs.Milliseconds);
            Debug.Log(LogStore.Label + " logs write to file in " + writeElapsedTime);
        }).Start();
    }

    public void WriteHeaders()
    {
        using (var file = new StreamWriter(filePath, true))
        {
            file.WriteLine(LogStore.GenerateHeaders());
        }
    }

    public void WriteLine(string line, WriteMode writeMode = WriteMode.Append)
    {
        if (writeMode == WriteMode.Append)
        {
            using (var file = new StreamWriter(filePath, true))
            {
                file.Write(line);
            }
        }
    }

}