using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Net;
using System.IO;
using System.Text;

public class htmltest : MonoBehaviour
{

    public bool query;

#if UNITY_EDITOR

    // Update is called once per frame
    void Update()
    {
        if (query)
        {
            query = false;

            string result = null;
            string url = "http://bulk-out-version.pumpfactory.xyz";  
            WebResponse response = null;
            StreamReader reader = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                response = request.GetResponse();
                reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8); 
                result = reader.ReadToEnd();

                Debug.Log(result);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                if (response != null)
                    response.Close();
            }
        }
    }
#endif

}
