using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.LoggingManager
{
    class LogCollection
    {
        public string label;
        public string email;
        private string sessionID;
        public bool createLogStringOverTime;
        public int count = 0;
        public bool saveHeaders = true;
        public Dictionary<string, List<object>> logs = new Dictionary<string, List<object>>();
        private bool logsInitiated;
        public string dataString;

        public LogCollection(string label, bool createLogStringOverTime, string email)
        {
            Init(label, email,Guid.NewGuid().ToString(),createLogStringOverTime);
        }

        public LogCollection(string label, string sessionID, bool createLogStringOverTime, string email )
        {
            Init(label, email, sessionID, createLogStringOverTime);
        }

        public LogCollection(string label, string sessionID, bool createLogStringOverTime)
        {
            Init(label, "anonymous", sessionID, createLogStringOverTime);
        }

        public LogCollection(string label, bool createLogStringOverTime)
        {
            Init(label, "anonymous", Guid.NewGuid().ToString(), createLogStringOverTime);
        }

        private void Init(string label, string email, string sessionID, bool createLogStringOverTime)
        {
            this.label = label;
            this.email = email;
            saveHeaders = true;
            logsInitiated = false;
            this.sessionID = sessionID;
            this.createLogStringOverTime = createLogStringOverTime;
        }

        public void InitLogs()
        {
            if (logsInitiated)
            {
                return;
            }
            if (!logs.ContainsKey("Timestamp"))
            {
                logs["Timestamp"] = new List<object>();
            }
            if (!logs.ContainsKey("Framecount"))
            {
                logs["Framecount"] = new List<object>();
            }
            if (!logs.ContainsKey("SessionID"))
            {
                logs["SessionID"] = new List<object>();
            }
            if (!logs.ContainsKey("Email"))
            {
                logs["Email"] = new List<object>();
            }
            logsInitiated = true;
        }

        public void AddAllLogs(Dictionary<string, List<string>> logData)
        {
            logs = logData.ToDictionary(
                x => x.Key.ToString(),
                x => x.Value.ConvertAll(y => (object)y)
                );
            count = logs.ElementAt(0).Value.Count;
        }

        private void LogCommonColumns()
        {
            InitLogs();
            logs["Timestamp"].Add(GetTimeStamp());
            logs["Framecount"].Add(GetFrameCount());
            logs["SessionID"].Add(sessionID);
            logs["Email"].Add(email);
        }

        private void CreateOrAppendToLogs(string columnName, object value)
        {
            if (logs.TryGetValue(columnName, out List<object> list))
            {
                list.Add(value);
            }
            else
            {
                logs[columnName] = new List<object>
                {
                    value
                };
                
            }
        }

        private void AddLineToDataString(string columnLabel, object value)
        {
            if (value != null)
            {
                dataString += ConnectToMySQL.ConvertToString(value, columnLabel);
            }
            else
            {
                dataString += "null";
            }
        }

        public void AddLog(string columnLabel,object value)
        {
            LogCommonColumns();
            CreateOrAppendToLogs(columnLabel, value);
            if (createLogStringOverTime)
            {
                AddLineToDataString(columnLabel,value);
                dataString += ";";
            }
            count++;
        }

        public void AddLog(Dictionary<string, object> logData)
        {
            LogCommonColumns();
            if (createLogStringOverTime)
            {
                int nbElements = logData.Count();
                int i = 0;
                foreach (KeyValuePair<string, object> pair in logData)
                {
                    i++;
                    CreateOrAppendToLogs(pair.Key, pair.Value);
                    AddLineToDataString(pair.Key, pair.Value);
                    if (i != nbElements)
                    {
                        dataString += ",";
                    }
                }
                dataString += ";";
            }
            else
            {
                foreach (KeyValuePair<string, object> pair in logData)
                {
                    CreateOrAppendToLogs(pair.Key, pair.Value);
                }
            }
            count++;
        }

        // Generates the headers in a CSV format and saves them to the CSV file
        private string GenerateHeaders(char fieldSeperator)
        {
            string headers = "";
            foreach (string key in logs.Keys)
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
                    foreach (KeyValuePair<string, List<object>> log in logs)
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
