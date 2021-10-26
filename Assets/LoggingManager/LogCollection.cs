using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.LoggingManager
{
    class LogCollection
    {
        public string label;
        public int count = 0;
        public bool saveHeaders = true;
        public Dictionary<string, List<object>> log = new Dictionary<string, List<object>>();
        bool logsInitiated;

        public LogCollection(string label)
        {
            this.label = label;
            saveHeaders = true;
            logsInitiated = false;
        }


        public void InitLogs()
        {
            if (logsInitiated)
            {
                return;
            }
            if (!log.ContainsKey("Timestamp"))
            {
                log["Timestamp"] = new List<object>();
            }
            if (!log.ContainsKey("Framecount"))
            {
                log["Framecount"] = new List<object>();
            }
            if (!log.ContainsKey("SessionID"))
            {
                log["SessionID"] = new List<object>();
            }
            if (!log.ContainsKey("Email"))
            {
                log["Email"] = new List<object>();
            }
            logsInitiated = true;
        }

        public void AddAllLogs(Dictionary<string, List<string>> logData)
        {
            log = logData.ToDictionary(
                x => x.Key.ToString(),
                x => x.Value.ConvertAll(y => (object)y)
                );
        }

        private void LogCommonColumns(string sessionID, string email)
        {
            InitLogs();
            log["Timestamp"].Add(GetTimeStamp());
            log["Framecount"].Add(GetFrameCount());
            log["SessionID"].Add(sessionID);
            log["Email"].Add(email);
        }

        private void CreateOrAppendToLogs(string columnName, object value)
        {
            if (log.TryGetValue(columnName, out List<object> list))
            {
                list.Add(value);
            }
            else
            {
                log[columnName] = new List<object>();
                log[columnName].Add(value);
            }
        }

        public void AddLog(string columnLabel,object value, string sessionID, string email)
        {
            LogCommonColumns(sessionID, email);
            CreateOrAppendToLogs(columnLabel, value);
            count++;
        }

        public void AddLog(Dictionary<string, object> logData, string sessionID, string email)
        {
            LogCommonColumns(sessionID, email);
            foreach (KeyValuePair<string, object> pair in logData)
            {
                CreateOrAppendToLogs(pair.Key, pair.Value);
            }
            count++;
        }

        // Generates the headers in a CSV format and saves them to the CSV file
        private string GenerateHeaders(char fieldSeperator)
        {
            string headers = "";
            foreach (string key in log.Keys)
            {
                if (headers != "")
                {
                    headers += fieldSeperator;
                }
                headers += key;
            }
            return headers;
        }

        // Formats the logs to a CSV row format and saves them. Calls the CSV headers generation beforehand.
        // If a parameter doesn't have a value for a given row, uses the given value given previously (see 
        // UpdateHeadersAndDefaults).
        public void SaveToCSV(char fieldSeperator, string saveFullPath, string savePath, 
            string filePrefix, string filestamp, string fileExtension)
        {
            string headerLine = "";
            if (saveHeaders)
            {
                headerLine = GenerateHeaders(fieldSeperator);
            }
            string filename = label;
            string filePath = saveFullPath == "" ? savePath + "/" + filePrefix + filestamp + filename + fileExtension : saveFullPath;
            using (var file = new StreamWriter(filePath, true))
            {
                if (saveHeaders)
                {
                    file.WriteLine(headerLine);
                    saveHeaders = false;
                }
                for (int i = 0; i < count; i++)
                {
                    string line = "";
                    foreach (KeyValuePair<string, List<object>> log in log)
                    {
                        if (line != "")
                        {
                            line += fieldSeperator;
                        }

                        if (log.Value.ElementAtOrDefault(i) != null)
                        {

                            line += log.Value[i];
                        }
                        else
                        {
                            line += "NULL";
                        }
                    }
                    file.WriteLine(line);
                }
            }
            Debug.Log(label + " logs with " + count + 1 + " rows saved to " + filePath);
        }

        // Returns a time stamp including the milliseconds.
        public static string GetTimeStamp()
        {
            return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
        }

        public static string GetFrameCount()
        {
            return Time.frameCount == null ? "-1" : Time.frameCount.ToString();
        }

    }
}
