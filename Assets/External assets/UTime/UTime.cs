using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

    public class UTime : MonoBehaviour
    {
        private const string TimeServer = "https://script.google.com/macros/s/AKfycbyal_mx91_jytjMzr_ykoP3NfZXBVMNRNXCX7qmt0QpTj6mAHg/exec";
        private static UTime Instance;

        void Awake()
        {
            if (!Instance) { 
               Instance = this;
            }
        }

        /// <summary>
        /// Get time from server asynchronically with callback (bool success, string error, DateTime time)
        /// </summary>
        public void GetUtcTimeAsync(Action<bool, string, DateTime> callback)
        {
            Debug.Log("Requesting network time from: " + TimeServer);
            StartCoroutine(Download(TimeServer, www =>
                {
                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            callback(true, null, DateTime.Parse(www.downloadHandler.text).ToUniversalTime());
                        }
                        catch (Exception e)
                        {
                            callback(false, e.Message, DateTime.MinValue);
                        }
                    }
                    else
                    {
                        callback(false, www.error, DateTime.MinValue);
                    }
                }));
            
        }

        /// <summary>
        /// Check network connection asynchronically with callback (bool success)
        /// </summary>
        public void HasConnection(Action<bool> callback)
        {
            
                StartCoroutine(Download(TimeServer, www => { callback(www.result == UnityWebRequest.Result.Success); }));
            
        }

        private IEnumerator Download(string url, Action<UnityWebRequest> callback)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();
                callback(request);
            }
        }
    }
