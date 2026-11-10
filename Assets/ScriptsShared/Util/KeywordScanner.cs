using Swole;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[ExecuteInEditMode]
public class KeywordScanner : MonoBehaviour
{

#if UNITY_EDITOR

    public bool scan = false;

    public bool caseSensitive = false;

    public List<string> keywords = new List<string>(); 

    [Tooltip("Relative to Assets/")]
    public List<string> relativeDirectories = new List<string>();

    public List<string> directoryIgnoreKeywords = new List<string>();

    [Serializable]
    public struct Match
    {

        public string header;

        public string keyword;

        public string path;

        public string fileName;

        public int lineIndex;

        public int startIndex;

        public string lineText;

    }

    public List<Match> results = new List<Match>(); 

    // Update is called once per frame
    void Update()
    {
        if (scan)
        {
            scan = false;
            Scan();
        }
    }

    public void Scan()
    {

        if (results == null) results = new List<Match>();

        results.Clear();

        foreach(string relDir in relativeDirectories)
        {
            if (string.IsNullOrEmpty(relDir)) continue;

            DirectoryInfo absDir = new DirectoryInfo(Path.Combine(Application.dataPath, relDir));
            if (absDir == null) continue;

            var files = absDir.EnumerateFiles("*", SearchOption.AllDirectories);
            Debug.Log($"Scanning {absDir}");
            foreach (var file in files)
            {
                string path = file.FullName;
                string fileName = file.Name;
                bool skip = false;
                foreach(string ignore in directoryIgnoreKeywords)
                {
                    string ignore_ = caseSensitive ? ignore : ignore.AsID();
                    if ((caseSensitive ? path : path.AsID()).IndexOf(ignore) > 0)
                    {
                        skip = true;
                        break;
                    }
                }
                if (skip) continue;
                string[] lines = File.ReadAllLines(path); 
                if (lines != null)
                {
                    for(int a = 0; a < lines.Length; a++)
                    {
                        string line = caseSensitive ? lines[a] : lines[a].AsID();
                        foreach(string keyword in keywords)
                        {
                            if (string.IsNullOrEmpty(keyword)) continue;
                            string keyword_ = caseSensitive ? keyword : keyword.AsID();
                            int ind = line.IndexOf(keyword_);
                            if (ind >= 0)
                            {
                                results.Add(new Match() { header = $"{keyword} - {fileName}", keyword = keyword, path = path, fileName = fileName, lineIndex = a,  startIndex = ind, lineText = lines[a] });
                            }
                        }
                    }
                }
            }
        }

    }

#endif

}
