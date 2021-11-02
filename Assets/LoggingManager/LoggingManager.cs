using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Globalization;
using System.Linq;

public enum LogMode {
    Append,
    Overwrite
}

public class LoggingManager : MonoBehaviour
{
    // sampleLog[COLUMN NAME][COLUMN NO.] = [OBJECT] (fx a float, int, string, bool)
    private Dictionary<string, LogStore> logsList = new Dictionary<string, LogStore>();

	[Header("Logging Settings")]
    [Tooltip("The Meta Collection will contain a session ID, a device ID and a timestamp.")]
    [SerializeField]
    private bool CreateMetaCollection = true;

	[Header("MySQL Save Settings")]
    [SerializeField]
    private bool enableMySQLSave = true;
    [SerializeField]
    private string email = "anonymous";

    [SerializeField]
    private ConnectToMySQL connectToMySQL;


	[Header("CSV Save Settings")]
    [SerializeField]
    private bool enableCSVSave = true;

    [Header("Log string Settings")]
    [SerializeField]
    private bool logStringOverTime = true;

    [Tooltip("If save path is empty, it defaults to My Documents.")]
    [SerializeField]
    private string savePath = "";

    [SerializeField]
    private string filePrefix = "log";

    [SerializeField]
    private string fileExtension = ".csv";

    private string filePath;
    private char fieldSeperator = ';';
    private string sessionID = "";
    private string deviceID = "";
    private string filestamp;

    // Start is called before the first frame update
    void Awake()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        NewFilestamp();
        if (savePath == "") {
            savePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        }
    }

    public void SetSavePath(string path)
    {
        this.savePath = path;
    }

    public void GenerateUIDs() {
        sessionID = Md5Sum(System.DateTime.Now.ToString(SystemInfo.deviceUniqueIdentifier + "yyyy:MM:dd:HH:mm:ss.ffff").Replace(" ", "").Replace("/", "").Replace(":", ""));
        deviceID = SystemInfo.deviceUniqueIdentifier;
    }

 
    public void NewFilestamp() {
       // filestamp = GetTimeStamp().Replace('/', '-').Replace(":", "-");

        if (CreateMetaCollection) {
            GenerateUIDs();
            Log("Meta", "SessionID", sessionID, LogMode.Overwrite);
            Log("Meta", "DeviceID", deviceID, LogMode.Overwrite);
        }
    }

    public void SaveLog(string collectionLabel) {
        if (logsList.ContainsKey(collectionLabel)) {
            if (Application.platform != RuntimePlatform.WebGLPlayer) {
                SaveToCSV(collectionLabel);
            }
            SaveToSQL(collectionLabel);
        } else {
            Debug.LogError("No Collection Called " + collectionLabel);
        }
    }

    public void SaveAllLogs()
    {
        foreach (KeyValuePair<string, LogStore> pair in logsList)
        {
            SaveLog(pair.Key);
        }
    }


    public void SetEmail(string newEmail) {
        email = newEmail;
    }

    public void CreateLog(string collectionLabel)
    {
        LogStore logStore = new LogStore(collectionLabel, email, sessionID, logStringOverTime);
        logsList.Add(collectionLabel, logStore);
        if (CreateMetaCollection)
        {
            LogStore metaLog = logStore.CreateAssociatedMetaLog();
            logsList.Add("Meta",metaLog);
            metaLog.Add("SessionID", sessionID);
            metaLog.Add("DeviceID", deviceID);
        }
    }


    public void Log(string collectionLabel, Dictionary<string, object> logData, LogMode logMode=LogMode.Append) {
        if (!logsList.ContainsKey(collectionLabel)) {
            logsList.Add(collectionLabel, new LogStore(collectionLabel, email,sessionID,logStringOverTime));
        }

        LogStore logStore = logsList[collectionLabel];

        foreach (KeyValuePair<string, object> pair in logData) {
            logStore.Add(pair.Key,pair.Value);
        }
        logStore.TerminateRow();

    }

    public void Log(string collectionLabel, string columnLabel, object value, LogMode logMode = LogMode.Append) {
        if (!logsList.ContainsKey(collectionLabel))
        {
            logsList.Add(collectionLabel, new LogStore(collectionLabel, email, sessionID, logStringOverTime));
        }
        LogStore logStore = logsList[collectionLabel];
        logStore.Add(columnLabel, value);
        logStore.TerminateRow();
    }

    public void ClearAllLogs() {
        foreach (KeyValuePair<string, LogStore> pair in logsList) {
            pair.Value.Clear();
        }
    }

    public void ClearLog(string collectionLabel) {
        if (logsList.ContainsKey(collectionLabel)) {
            logsList[collectionLabel].Clear();
        } else {
            Debug.LogError("Collection " + collectionLabel + " does not exist.");
        }
    }

    public void DeleteLog(string collectionLabel)
    {
        if (logsList.ContainsKey(collectionLabel))
        {
            logsList.Remove(collectionLabel);
        }
        else
        {
            Debug.LogError("Collection " + collectionLabel + " does not exist.");
        }
    }


    public void DeleteAllLogs()
    {
        foreach (var keyValuePair in logsList)
        {
            logsList.Remove(keyValuePair.Key);
        }
    }

    // Formats the logs to a CSV row format and saves them. Calls the CSV headers generation beforehand.
    // If a parameter doesn't have a value for a given row, uses the given value given previously (see 
    // UpdateHeadersAndDefaults).
    private void SaveToCSV(string label)
    {
        if(!enableCSVSave) return;
        if (logsList.TryGetValue(label,out LogStore logStore))
        {
            WriteToCSV writeToCsv = new WriteToCSV(logStore, savePath, filePrefix, fileExtension);
            writeToCsv.WriteAll();
        }
    }

    private void SaveToSQL(string label)
    {
        //if (!enableMySQLSave) { return; }

        //if (!logsList.ContainsKey(label)) {
        //    Debug.LogError("Could not find collection " + label + ". Aborting.");
        //    return;
        //}

        //if (logsList[label].log.Keys.Count == 0) {
        //    Debug.LogError("Collection " + label + " is empty. Aborting.");
        //    return;
        //}
        
        //connectToMySQL.AddToUploadQueue(logsList[label].log, logsList[label].label);    
        //connectToMySQL.UploadNow();
    }

    public string Md5Sum(string strToEncrypt)
    {
        System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
        byte[] bytes = ue.GetBytes(strToEncrypt);
    
        // encrypt bytes
        System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        byte[] hashBytes = md5.ComputeHash(bytes);
    
        // Convert the encrypted bytes back to a string (base 16)
        string hashString = "";
    
        for (int i = 0; i < hashBytes.Length; i++)
        {
            hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
        }
    
        return hashString.PadLeft(32, '0');
    }


}
