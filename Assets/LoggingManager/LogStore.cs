using System;
using System.CodeDom;
using System.Collections.Generic;
using Boo.Lang.Runtime;
using UnityEngine;

public class LogStore : MonoBehaviour
{

    private Dictionary<string, List<string>> logs;
    private string logString;

    private int nbLines;
    private bool createStringOverTime;
    private string currentLineLogged;
    private const string fieldSeparator = ";";
    private const string lineSeperator = "\n";

    public LogStore(bool createStringOverTime = false)
    {
        logs = new Dictionary<string, List<string>>();
        logString = "";
        currentLineLogged = "";
        this.createStringOverTime = createStringOverTime;
    }

    public void Create(List<string> columns)
    {
        columns.ForEach(column =>
        {
            Create(column);
        });
    }

    public void Create(string column)
    {
        logs.Add(column, new List<string>());
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
            currentLineLogged += fieldSeparator;
        }
        currentLineLogged += dataStr;
    }

    public void TerminateRow()
    {
        currentLineLogged += lineSeperator;
        logString += currentLineLogged;
        currentLineLogged = "";
        nbLines++;
    }


    public void Clear()
    {
        logs.Clear();
        logString = "";
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
            for (int i = 0; i < nbLines; i++)
            {
                string line = "";
                foreach (var pair in logs)
                {
                    if (line.Length == 0)
                    {
                        line += fieldSeparator;
                    }
                    line += pair.Value[i];
                }

                logString += (line + lineSeperator);
            }
        }
        return logString;
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
        return arg switch
        {
            float f => f.ToString("0.0000").Replace(",", "."),
            int => arg.ToString(),
            bool b => b ? "TRUE" : "FALSE",
            Vector3 vector3 => vector3.ToString("0.0000").Replace(",", "."),
            _ => arg.ToString(),
        };
    }

}