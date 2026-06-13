using System.Collections;
using UnityEngine;

#if UNITY_IOS
using UnityEngine.iOS;
#endif

#if UNITY_ANDROID
using Google.Play.Review;
#endif

public class InAppReviewManager : MonoBehaviour
{
    public static InAppReviewManager Instance;

    [Header("Gameplay timer")]
    [SerializeField] private bool countOnlyWhenPlaying = true;
    [SerializeField] private bool gameplayActive = true;

    [Header("Review timing (seconds)")]
    [SerializeField] private float minSecondsBeforeAsk = 60f;   // 1 минута
    [SerializeField] private float maxSecondsBeforeAsk = 180f;  // 3 минуты

    [Header("Safety")]
    [SerializeField] private bool askOnlyOnceEver = true;

    private const string AskedKey = "in_app_review_asked";
    private const string TargetTimeKey = "in_app_review_target_time";

    private float playedSeconds;
    private float targetSeconds;
    private bool isRequestInProgress;
    private bool hasAskedThisSession;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitTargetTime();
    }

    private void Update()
    {
        if (askOnlyOnceEver && PlayerPrefs.GetInt(AskedKey, 0) == 1)
            return;

        if (hasAskedThisSession || isRequestInProgress)
            return;

        if (!countOnlyWhenPlaying || gameplayActive)
        {
            playedSeconds += Time.unscaledDeltaTime;
        }

        if (playedSeconds >= targetSeconds)
        {
            hasAskedThisSession = true;
            StartCoroutine(RequestReviewCoroutine());
        }
    }

    private void InitTargetTime()
    {
        if (PlayerPrefs.HasKey(TargetTimeKey))
        {
            targetSeconds = PlayerPrefs.GetFloat(TargetTimeKey);
        }
        else
        {
            targetSeconds = Random.Range(minSecondsBeforeAsk, maxSecondsBeforeAsk);
            PlayerPrefs.SetFloat(TargetTimeKey, targetSeconds);
            PlayerPrefs.Save();
        }
    }

    [ContextMenu("Force Request Review")]
    public void ForceRequestReview()
    {
        if (isRequestInProgress)
            return;

        hasAskedThisSession = true;
        StartCoroutine(RequestReviewCoroutine());
    }

    /// <summary>
    /// Call this method only if need stop/start real gameplay
    /// </summary>
    public void SetGameplayActive(bool active)
    {
        gameplayActive = active;
    }

    /// <summary>
    /// Reset Review.
    /// </summary>
    [ContextMenu("Reset Review State")]
    public void ResetReviewState()
    {
        playedSeconds = 0f;
        hasAskedThisSession = false;
        isRequestInProgress = false;

        PlayerPrefs.DeleteKey(AskedKey);

        targetSeconds = Random.Range(minSecondsBeforeAsk, maxSecondsBeforeAsk);
        PlayerPrefs.SetFloat(TargetTimeKey, targetSeconds);
        PlayerPrefs.Save();

        Debug.Log($"[InAppReview] Reset done. New target time: {targetSeconds} sec");
    }

    private IEnumerator RequestReviewCoroutine()
    {
        isRequestInProgress = true;

#if UNITY_EDITOR
        Debug.Log("[InAppReview] Editor mode: review dialog is not shown.");
        MarkAsAsked();
        isRequestInProgress = false;
        yield break;

#elif UNITY_IOS
        Debug.Log("[InAppReview] Requesting iOS store review...");
        Device.RequestStoreReview();

        // Apple не гарантирует показ окна.
        MarkAsAsked();
        isRequestInProgress = false;
        yield break;

#elif UNITY_ANDROID
        Debug.Log("[InAppReview] Requesting Android in-app review...");

        var reviewManager = new ReviewManager();
        var requestFlowOperation = reviewManager.RequestReviewFlow();

        yield return requestFlowOperation;

        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning("[InAppReview] RequestReviewFlow failed: " + requestFlowOperation.Error);
            isRequestInProgress = false;
            yield break;
        }

        PlayReviewInfo playReviewInfo = requestFlowOperation.GetResult();

        var launchFlowOperation = reviewManager.LaunchReviewFlow(playReviewInfo);
        yield return launchFlowOperation;

        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning("[InAppReview] LaunchReviewFlow failed: " + launchFlowOperation.Error);
            isRequestInProgress = false;
            yield break;
        }

        // Даже при успешном вызове окно могло не показаться — это нормально.
        MarkAsAsked();
        isRequestInProgress = false;
        yield break;

#else
        Debug.Log("[InAppReview] Unsupported platform.");
        isRequestInProgress = false;
        yield break;
#endif
    }

    private void MarkAsAsked()
    {
        if (askOnlyOnceEver)
        {
            PlayerPrefs.SetInt(AskedKey, 1);
        }

        PlayerPrefs.Save();
        Debug.Log("[InAppReview] Marked as asked.");
    }
}