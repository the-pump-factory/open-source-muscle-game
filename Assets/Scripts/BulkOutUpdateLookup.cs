using System;
using System.Net;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity;
using Swole;

public class BulkOutUpdateLookup : MonoBehaviour
{

    private const string FMT = "O";
    private static string DateToString(DateTime date) => date.ToString(FMT);
    private static DateTime DateFromString(string strDate) => DateTime.ParseExact(strDate, FMT, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    

    public const string _lookupUrl = @"http://bulk-out-version.pumpfactory.xyz";
    public const float lookupFrequency = 2;
    public const string lookupSettingsFileName = "updates.json";

    protected Version currentVersion, latestVersion;

    public GameObject linkTextRoot;

    [Serializable]
    public struct LookupSettings
    {
        public string lastLookup;
        public DateTime LastLookup => DateFromString(lastLookup);

        public string latestVersion;
        public string downloadLink;

        public static LookupSettings Default => new LookupSettings() { lastLookup = DateToString(DateTime.MinValue) };
    }

    private LookupSettings lookupSettings; 
    private string dataPath;

    void Awake()
    {
        dataPath = Application.persistentDataPath;
        currentVersion = latestVersion = new Version(Application.version);
        lookupSettings = LookupSettings.Default;
    }
    void Start()
    {
        var lookupSettingsPath = Path.Combine(dataPath, lookupSettingsFileName);
        try
        {
            if (File.Exists(lookupSettingsPath))
            {
                var bytes = File.ReadAllBytes(lookupSettingsPath);
                var json = DefaultJsonSerializer.StringEncoder.GetString(bytes);
                lookupSettings = swole.FromJson<LookupSettings>(json);

                if (!string.IsNullOrWhiteSpace(lookupSettings.latestVersion)) latestVersion = new Version(lookupSettings.latestVersion);
                 
                if ((DateTime.Now - lookupSettings.LastLookup).TotalHours < lookupFrequency) return;    
            }
        }
        catch { }

        PullMetadata();
    }

    void Update()
    {
        if (linkTextRoot != null && currentVersion < latestVersion && !linkTextRoot.activeSelf)
        {
            if (!string.IsNullOrWhiteSpace(lookupSettings.downloadLink))
            {
                linkTextRoot.SetActive(true);
                CustomEditorUtils.SetButtonOnClickAction(linkTextRoot, () => Application.OpenURL(lookupSettings.downloadLink)); 
            }
        }
    }

    protected void PullMetadata()
    {
        Debug.Log("Checking for updates...");

        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_lookupUrl);
            request.Method = "GET";
            request.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
            request.BeginGetResponse(new AsyncCallback(ReceiveMetadata), request);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR
            Swole.swole.LogError(ex);
#else
            Swole.swole.LogWarning("Failed to retrieve update metadata!");
            Swole.swole.LogWarning(ex.Message);
#endif
        }
    }

    protected void ReceiveMetadata(IAsyncResult result)
    {
        WebResponse response = null;
        StreamReader reader = null;

        try
        {
            HttpWebRequest request = (HttpWebRequest)result.AsyncState;
            response = request.EndGetResponse(result);
            reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            ParseMetadata(reader.ReadToEnd());

            lookupSettings.lastLookup = DateToString(DateTime.Now);
            var lookupSettingsPath = Path.Combine(dataPath, lookupSettingsFileName);
            var json = swole.ToJson(lookupSettings, true);
            File.WriteAllBytes(lookupSettingsPath, DefaultJsonSerializer.StringEncoder.GetBytes(json));

            Debug.Log("Successfully received update metadata!");
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR
            Swole.swole.LogError(ex);
#else
            Swole.swole.LogWarning("Failed to retrieve update metadata!");
            Swole.swole.LogWarning(ex.Message);
#endif
        }
        finally
        {
            if (reader != null)
                reader.Close();
            if (response != null)
                response.Close();
        }
    }

    protected bool TryExtractHTMLElement(string metadata, string elementName, out string elementData, out string editedMetaData)
    {
        elementData = string.Empty;
        editedMetaData = metadata;

        string startTag = $"<{elementName}>";
        string endTag = $"</{elementName}>";

        var startIndex = metadata.ToLower().IndexOf(startTag.ToLower());
        var endIndex = metadata.ToLower().IndexOf(endTag.ToLower());

        if (startIndex >= 0)
        {
            if (endIndex < 0) endIndex = metadata.Length;
            elementData = metadata.Substring(startIndex + startTag.Length, endIndex - (startIndex + startTag.Length));  
            editedMetaData = metadata.Substring(0, startIndex) + (endIndex >= metadata.Length ? string.Empty : metadata.Substring(endIndex + endTag.Length));
        }

        return false;
    } 
    protected void ParseMetadata(string metadata)
    {
        Debug.Log(metadata);

        try
        {
            TryExtractHTMLElement(metadata, "version", out string versionString, out metadata);
            if (!string.IsNullOrWhiteSpace(versionString))
            {
                lookupSettings.latestVersion = versionString;
                latestVersion = new Version(versionString);
            }

            TryExtractHTMLElement(metadata, "download", out string downloadLink, out metadata);
            if (!string.IsNullOrWhiteSpace(downloadLink))
            {
                lookupSettings.downloadLink = downloadLink;
            }
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR
            Swole.swole.LogError(ex);
#else
            Swole.swole.LogWarning("Failed to parse update metadata!");
            Swole.swole.LogWarning(ex.Message);
#endif
        }
    }

}
