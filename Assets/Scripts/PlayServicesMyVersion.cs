#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
using UnityEngine;

public class PlayServicesMyVersion : MonoBehaviour {

	// Use this for initialization
	void Awake () {
#if UNITY_ANDROID && !UNITY_EDITOR
	    PlayGamesPlatform.DebugLogEnabled = true;
	    PlayGamesPlatform.Activate();
#endif
	}

    public void SignInCallback(bool success)
    {
        if (success)
        {
            print("yey");
        }
        else
        {
            print("err");
        }
    }

    public void signIn()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!PlayGamesPlatform.Instance.localUser.authenticated)
        {
            // Sign in with Play Game Services, showing the consent dialog
            // by setting the second parameter to isSilent=false.
            print("call back...");
            PlayGamesPlatform.Instance.Authenticate(status => SignInCallback(status == SignInStatus.Success));
        }
        else
        {
            print("already In");
        }
#endif

    }
    

    public void unlockAchievement(string id)
    {
        unlockPartialAchievement(id, 100);
    }

    public void unlockPartialAchievement(string id, double pourcentage)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (PlayGamesPlatform.Instance.localUser.authenticated)
            PlayGamesPlatform.Instance.ReportProgress(id, pourcentage, success => { });
#endif
    }

    public bool showAchievementUI()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!PlayGamesPlatform.Instance.localUser.authenticated) { signIn(); return false; }
        PlayGamesPlatform.Instance.ShowAchievementsUI();
        return true;
#else
        return false;
#endif
    }

    public void addValueToLeaderbord(string id, int score)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (PlayGamesPlatform.Instance.localUser.authenticated)
            PlayGamesPlatform.Instance.ReportScore(score, id, success => { });
#endif
    }

    public bool showLeaderbordUI()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!PlayGamesPlatform.Instance.localUser.authenticated) { signIn(); return false; }
        PlayGamesPlatform.Instance.ShowLeaderboardUI();
        return true;
#else
        return false;
#endif
    }

    public void showQuest()
    {
    }
}
