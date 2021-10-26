using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Globalization;
using System.Linq;
using Assets.LoggingManager;

public enum LogMode {
    Append,
    Overwrite
}


public class LoggingManager : MonoBehaviour
{
    private Dictionary<string, string> statelogs = new Dictionary<string, string>();
    private Dictionary<string, Dictionary<int, string>> logs = new Dictionary<string, Dictionary<int, string>>();

    // sampleLog[COLUMN NAME][COLUMN NO.] = [OBJECT] (fx a float, int, string, bool)
    private Dictionary<string, LogCollection> collections = new Dictionary<string, LogCollection>();

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
    private bool enableCSVSave = false;

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

    private string saveFullPath;

    // Start is called before the first frame update
    void Awake()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        NewFilestamp();
        if (savePath == "") {
            savePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        }
    }

    public void SetSaveFullPath(string saveFullPath)
    {
        this.saveFullPath = saveFullPath;
    }

    public void GenerateUIDs() {
        sessionID = Md5Sum(System.DateTime.Now.ToString(SystemInfo.deviceUniqueIdentifier + "yyyy:MM:dd:HH:mm:ss.ffff").Replace(" ", "").Replace("/", "").Replace(":", ""));
        deviceID = SystemInfo.deviceUniqueIdentifier;
    }

    public Dictionary<string, List<object>> GetLog(string collectionLabel) {
        return new Dictionary<string, List<object>>(collections[collectionLabel].log);
    }

    public void SaveAllLogs() {
        foreach(KeyValuePair<string, LogCollection> pair in collections) {
            SaveLog(pair.Value.label);
        }
    }

    public void NewFilestamp() {
        filestamp = LogCollection.GetTimeStamp().Replace('/', '-').Replace(":", "-");

        if (CreateMetaCollection) {
            GenerateUIDs();
            Log("Meta", "SessionID", sessionID, LogMode.Overwrite);
            Log("Meta", "DeviceID", deviceID, LogMode.Overwrite);
        }

        foreach(KeyValuePair<string, LogCollection> pair in collections) {
            pair.Value.saveHeaders = true;
        }
    }

    public void SaveLog(string collectionLabel) {
        if (collections.ContainsKey(collectionLabel)) {
            if (Application.platform != RuntimePlatform.WebGLPlayer) {
                SaveToCSV(collectionLabel);
            }
            SaveToSQL(collectionLabel);
        } else {
            Debug.LogError("No Collection Called " + collectionLabel);
        }
    }

    public void SetEmail(string newEmail) {
        email = newEmail;
    }

    public void CreateLog(string collectionLabel) {
        collections.Remove(collectionLabel);
        collections.Add(collectionLabel, new LogCollection(collectionLabel));
    }

    public void SetupLog(string collectionLabel, bool newLogs = false)
    {
        if (newLogs)
        {
            collections.Remove(collectionLabel);
            collections.Add(collectionLabel, new LogCollection(collectionLabel));
        }
        else if (!collections.ContainsKey(collectionLabel))
        {
            collections.Add(collectionLabel, new LogCollection(collectionLabel));
        }
        collections[collectionLabel].InitLogs();
    }

    //This function has to be called when the logging is terminated.
    public void Log(string collectionLabel, Dictionary<string, List<string>> logData)
    {
        SetupLog(collectionLabel, true);
        collections[collectionLabel].AddAllLogs(logData);
    }

    //call this method each time a line is logged
    public void Log(string collectionLabel, Dictionary<string, object> logData) {
        collections[collectionLabel].AddLog(logData, sessionID, email);
    }

    //call this method each time a line is logged
    public void Log(string collectionLabel, string columnLabel, object value, LogMode logMode = LogMode.Append) {
        collections[collectionLabel].AddLog(columnLabel, value, sessionID, email);
    }

    public void ClearAllLogs() {
        foreach (KeyValuePair<string, LogCollection> pair in collections) {
            foreach (var key in collections[pair.Key].log.Keys.ToList()) {
                collections[pair.Key].log[key] = new List<object>();
            }
            collections[pair.Key].count = 0;
        }
    }

    public void ClearLog(string collectionLabel) {
        if (collections.ContainsKey(collectionLabel)) {
            foreach (var key in collections[collectionLabel].log.Keys.ToList()) {
                collections[collectionLabel].log[key] = new List<object>();
            }
            collections[collectionLabel].count = 0;
        } else {
            Debug.LogError("Collection " + collectionLabel + " does not exist.");
            return;
        }
    }

    private void SaveToCSV(string label)
    {
        if(!enableCSVSave) return;
        collections[label].SaveToCSV(fieldSeperator, saveFullPath, 
            savePath, filePrefix, filestamp, fileExtension);
    }


    private void SaveToSQL(string label)
    {
        if (!enableMySQLSave) { return; }

        if (!collections.ContainsKey(label)) {
            Debug.LogError("Could not find collection " + label + ". Aborting.");
            return;
        }

        if (collections[label].log.Keys.Count == 0) {
            Debug.LogError("Collection " + label + " is empty. Aborting.");
            return;
        }
        
        //connectToMySQL.AddToUploadQueue(collections[label].log, collections[label].label);    
        connectToMySQL.UploadNow();
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

    // Converts the values of the parameters (in a "object format") to a string, formatting them to the
    // correct format in the process.
    private string ConvertToString(object arg)
    {
        if (arg is float)
        {
            return ((float)arg).ToString("0.0000").Replace(",", ".");
        } else if (arg is int) {
            return arg.ToString();
        } else if (arg is bool) {
            return ((bool)arg) ? "TRUE" : "FALSE";
        } else if (arg is Vector3)
        {
            return ((Vector3)arg).ToString("0.0000").Replace(",", ".");
        }
        else
        {
            return arg.ToString();
        }
    }



}
