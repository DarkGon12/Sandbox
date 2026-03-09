using System;
using System.Collections;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    public static event Action<bool> OnInternetStateChanged;

    private bool hasInternet;

    [SerializeField] private float checkInterval = 3f;

    public bool HasInternet => hasInternet;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CheckInternet();
        StartCoroutine(CheckRoutine());
    }

    private IEnumerator CheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            CheckInternet();
        }
    }

    private void CheckInternet()
    {
        bool internetNow =
            Application.internetReachability != NetworkReachability.NotReachable;

        if (internetNow != hasInternet)
        {
            hasInternet = internetNow;
            OnInternetStateChanged?.Invoke(hasInternet);

            Debug.Log("Internet state changed: " + hasInternet);
        }
    }
}