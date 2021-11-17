using System;
using System.CodeDom;
using System.Collections.Generic;
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

    private Dictionary<string, List<string>> logs;
    private StringBuilder logString;

    private LogType logType;
    private bool logCommonColumns;

    private int nbLines;
    private bool createStringOverTime;
    private StringBuilder currentLineLogged;
    private const string fieldSeparator = ";";
    private const string lineSeperator = "\n";
    private string email;
    public string SessionId { get; set; }


    public LogStore(string email, bool createStringOverTime,
        LogType logType = LogType.Normal, bool logCommonColumns = true)
    {
        Init(email, Guid.NewGuid().ToString(), createStringOverTime, logType, logCommonColumns);
    }

    public LogStore(string email, string sessionID, bool createStringOverTime,
        LogType logType = LogType.Normal, bool logCommonColumns = true)
    {
        Init(email, sessionID, createStringOverTime, logType, logCommonColumns);
    }

    private void Init(string email, string sessionID, bool createStringOverTime,
        LogType logType, bool logCommonColumns)
    {
        logs = new Dictionary<string, List<string>>();
        logString = new StringBuilder();
        currentLineLogged = new StringBuilder();
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
  
        string dataStr = ConvertToString(data);
        if (logs.TryGetValue(column, out List<string> list))
        {
            list.Add(dataStr);
        }
        else
        { 
            logs.Add(column, new List<string>());
            logs[column].Add(dataStr);
        }

        if (currentLineLogged.Length != 0)
        {
            currentLineLogged.Append(fieldSeparator);
        }
        currentLineLogged.Append(dataStr);
    }

    private void AddCommonColumns()
    {
        string timeStamp = GetTimeStamp();
        string frameCount = GetFrameCount();
        if (logs.TryGetValue("Timestamp", out List<string> list1))
        {
            list1.Add(timeStamp);
        }
        else
        {
            logs.Add("Timestamp", new List<string>());
            logs["Timestamp"].Add(timeStamp);
        }

        if (logs.TryGetValue("Framecount", out List<string> list2))
        {
            list2.Add(frameCount);
        }
        else
        {
            logs.Add("Framecount", new List<string>());
            logs["Framecount"].Add(frameCount);
        }

        if (logs.TryGetValue("SessionID", out List<string> list3))
        {
            list3.Add(SessionId);
        }
        else
        {
            logs.Add("SessionID", new List<string>());
            logs["SessionID"].Add(SessionId);
        }

        if (logs.TryGetValue("Email", out List<string> list4))
        {
            list4.Add(email);
        }
        else
        {
            logs.Add("Email", new List<string>());
            logs["Email"].Add(email);
        }

        if (createStringOverTime)
        {
            if (logType == LogType.Normal && logCommonColumns)
            {
                currentLineLogged.Insert(0, timeStamp + fieldSeparator + frameCount +
                                            fieldSeparator + SessionId + fieldSeparator + email + fieldSeparator);
            }
            else if (logType == LogType.Meta)
            {
                currentLineLogged.Insert(0, SessionId + fieldSeparator + email + fieldSeparator);
            }
        }
    }

    public void TerminateRow()
    {
        AddCommonColumns();
        currentLineLogged.Append(lineSeperator);
        logString.Append(currentLineLogged);
        currentLineLogged.Clear();
        nbLines++;
    }


    public void Clear()
    {
        logs.Clear();
        logString.Clear();
        nbLines = 0;
    }


    public T ExportAll<T>()
    {
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