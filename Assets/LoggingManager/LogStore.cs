using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Boo.Lang.Runtime;
using UnityEngine;


public enum LogType
{
    Normal,
    Meta
}

public class LogStore : MonoBehaviour
{

    private SortedDictionary<string, List<string>> logs;
    private StringBuilder logString;
    public string Label { get; set; }

    private LogType logType;
    private bool logCommonColumns;

    private int nbLines;
    private bool createStringOverTime;
    private StringBuilder currentLineLogged;
    private SortedDictionary<string, string> currentLogs;
    private const string fieldSeparator = ";";
    private const string lineSeperator = "\n";
    private string email;
    public string SessionId { get; set; }


    public LogStore(string label, string email, bool createStringOverTime,
        LogType logType = LogType.Normal, bool logCommonColumns = true)
    {
        Init(label, email, Guid.NewGuid().ToString(), createStringOverTime, logType, logCommonColumns);
    }

    public LogStore(string label, string email, string sessionID, bool createStringOverTime,
        LogType logType = LogType.Normal, bool logCommonColumns = true)
    {
        Init(label, email, sessionID, createStringOverTime, logType, logCommonColumns);
    }

    private void Init(string label, string email, string sessionID, bool createStringOverTime,
        LogType logType, bool logCommonColumns)
    {
        this.Label = label;
        logs = new SortedDictionary<string, List<string>>();
        logString = new StringBuilder();
        currentLineLogged = new StringBuilder();
        currentLogs = new SortedDictionary<string, string>();
        this.createStringOverTime = createStringOverTime;
        this.email = email;
        SessionId = sessionID;
        this.logType = logType;
        this.logCommonColumns = logCommonColumns;
        if (logType == LogType.Normal && logCommonColumns)
        {
            logs.Add("Timestamp", new List<string>());
            logs.Add("Framecount", new List<string>());
            logs.Add("SessionID", new List<string>());
            logs.Add("Email", new List<string>());
        }
        else if (logType == LogType.Meta)
        {
            logs.Add("SessionID", new List<string>());
            logs.Add("Email", new List<string>());
        }
    }


    public void Add(string column, object data)
    {
        if (nbLines > 0 && !logs.Keys.Contains(column))
        {
            logs.Add(column,new List<string>(Enumerable.Repeat("NULL", nbLines).ToList()));
            if (createStringOverTime)
            {
                Debug.LogError("Header " + column + " added durring logging process...\n" +
                               "aborting logging datastring on the fly");
                createStringOverTime = false;
                logString.Clear();
            }
        }

        if (currentLogs.ContainsKey(column))
        {
            TerminateRow();
        }
        string dataStr = ConvertToString(data);
        AddToDictIfNotExists(currentLogs,column,dataStr);
    }

    private void CreateOrAddToLogsDict(IDictionary<string, List<string>> dictionary, string key, string value)
    {
        if (dictionary.TryGetValue(key, out List<string> list))
        {
            list.Add(value);
        }
        else
        {
            dictionary.Add(key, new List<string>());
            dictionary[key].Add(value);
        }
    }

    private void AddToDictIfNotExists(IDictionary<string, string> dictionary, string key, string value)
    {
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key,value);
        }
    }

    private void AddCommonColumns()
    {
        string timeStamp = GetTimeStamp();
        string frameCount = GetFrameCount();
        if (logType == LogType.Normal && logCommonColumns)
        {
            AddToDictIfNotExists(currentLogs, "Timestamp", timeStamp);
            AddToDictIfNotExists(currentLogs, "Framecount", frameCount);
            AddToDictIfNotExists(currentLogs, "SessionID", SessionId);
            AddToDictIfNotExists(currentLogs, "Email", email);
        }
        else if (logType == LogType.Meta)
        {
            Debug.Log("************META : " + Label);
            AddToDictIfNotExists(currentLogs, "SessionID", SessionId);
            AddToDictIfNotExists(currentLogs, "Email", email);
        }
    }

    public void TerminateRow()
    {
        AddCommonColumns();
        foreach (var logsKey in logs.Keys)
        {
            if (!currentLogs.ContainsKey(logsKey))
            {
                currentLogs.Add(logsKey,"NULL");
            }
        }
        foreach (var pair in currentLogs)
        {
            CreateOrAddToLogsDict(logs,pair.Key,pair.Value);
            if (createStringOverTime)
            {
                if (currentLineLogged.Length != 0)
                {
                    currentLineLogged.Append(fieldSeparator);
                }
                currentLineLogged.Append(pair.Value);
            }
        }
        
        if (createStringOverTime)
        {
            currentLineLogged.Append(lineSeperator);
            logString.Append(currentLineLogged);
            currentLineLogged.Clear();
        }
        currentLogs.Clear();
        nbLines++;
    }


    public void Clear()
    {
        logs.Clear();
        currentLogs.Clear();
        logString.Clear();
        nbLines = 0;
    }


    public T ExportAll<T>()
    {
        if (currentLogs.Count != 0)
        {
            TerminateRow();
        }
        var type = typeof(T);
        if (type == typeof(Dictionary<string, string>))
        {
            return (T)Convert.ChangeType(logs, type);
        }
        if (type == typeof(string))
        {
            return (T)Convert.ChangeType(ExportToString(), type);
        }
        throw new RuntimeException("Export type must be a Dictionnary<string,string> or string");
    }

    private string ExportToString()
    {
        if (!createStringOverTime)
        {
            logString.Clear();
            for (int i = 0; i < nbLines; i++)
            {
                string line = "";
                foreach (string key in logs.Keys)
                {
                    if (line != "")
                    {
                        line += fieldSeparator;
                    }
                    line += logs[key][i];
                }

                logString.Append(line + lineSeperator);
            }
        }
        return logString.ToString();
    }

    public string GenerateHeaders()
    {
        string headers = "";
        foreach (string key in logs.Keys)
        {
            if (headers != "")
            {
                headers += fieldSeparator;
            }
            headers += key;
        }
        return headers;
    }

    public LogStore CreateAssociatedMetaLog()
    {
        return new LogStore("Meta", email, createStringOverTime, LogType.Meta, false);
    }

    // Converts the values of the parameters (in a "object format") to a string, formatting them to the
    // correct format in the process.
    private static string ConvertToString(object arg)
    {
        if (arg is float)
        {
            return ((float)arg).ToString("0.0000").Replace(",", ".");
        }
        if (arg is int)
        {
            return arg.ToString();
        }
        if (arg is bool)
        {
            return ((bool)arg) ? "TRUE" : "FALSE";
        }
        if (arg is Vector3)
        {
            return ((Vector3)arg).ToString("0.0000").Replace(",", ".");
        }
        return arg.ToString();
    }

    private string GetTimeStamp()
    {
        return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
    }

    private string GetFrameCount()
    {
        return Time.frameCount == 0 ? "-1" : Time.frameCount.ToString();
    }

}