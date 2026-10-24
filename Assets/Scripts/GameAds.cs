using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

namespace Catcheep.Services
{
    public enum ShowResult { Failed, Skipped, Finished }

    public sealed class ShowOptions
    {
        public Action<ShowResult> resultCallback;
    }

    // Preserves game reward rules while using the SDK 4 load/show lifecycle.
    public sealed class GameAds : MonoBehaviour, IUnityAdsInitializationListener,
        IUnityAdsLoadListener, IUnityAdsShowListener
    {
        private const string Rewarded = "rewardedVideo";
        private const string Interstitial = "video";
        private static GameAds instance;
        private readonly HashSet<string> loaded = new HashSet<string>();
        private Action<ShowResult> completion;
        private bool initializing;
        private bool showing;

        private static GameAds Instance
        {
            get
            {
                if (instance == null)
                {
                    var host = new GameObject("Advertising Service");
                    DontDestroyOnLoad(host);
                    instance = host.AddComponent<GameAds>();
                }
                return instance;
            }
        }

        public static void Initialize(string androidGameId)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var service = Instance;
            if (Advertisement.isInitialized) { service.LoadAds(); return; }
            if (service.initializing || !Advertisement.isSupported) return;
            service.initializing = true;
            Advertisement.Initialize(androidGameId, Debug.isDebugBuild, service);
#endif
        }

        public static bool IsReady(string adUnitId = Rewarded)
        {
            return instance != null && !instance.showing && instance.loaded.Contains(adUnitId);
        }

        public static void Show(string adUnitId = Interstitial, ShowOptions options = null)
        {
            if (!IsReady(adUnitId))
            {
                options?.resultCallback?.Invoke(ShowResult.Failed);
                return;
            }
            instance.loaded.Remove(adUnitId);
            instance.showing = true;
            instance.completion = options?.resultCallback;
            Advertisement.Show(adUnitId, instance);
        }

        public void OnInitializationComplete() { initializing = false; LoadAds(); }
        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            initializing = false;
            Debug.LogWarning("Ads unavailable: " + message);
        }

        private void LoadAds()
        {
            Advertisement.Load(Rewarded, this);
            Advertisement.Load(Interstitial, this);
        }

        public void OnUnityAdsAdLoaded(string adUnitId) { loaded.Add(adUnitId); }
        public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
        {
            loaded.Remove(adUnitId);
            Debug.LogWarning("Ad could not load (" + adUnitId + "): " + message);
        }

        public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState state)
        {
            Complete(state == UnityAdsShowCompletionState.COMPLETED ? ShowResult.Finished : ShowResult.Skipped);
            Advertisement.Load(adUnitId, this);
        }

        public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
        {
            Debug.LogWarning("Ad could not show: " + message);
            Complete(ShowResult.Failed);
            Advertisement.Load(adUnitId, this);
        }

        private void Complete(ShowResult result)
        {
            showing = false;
            var callback = completion;
            completion = null;
            if (callback?.Target is UnityEngine.Object owner && owner == null) return;
            callback?.Invoke(result);
        }

        public void OnUnityAdsShowStart(string adUnitId) { }
        public void OnUnityAdsShowClick(string adUnitId) { }
    }
}
