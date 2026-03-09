using UnityEngine;

public class InternetPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject _popup;

    private void OnEnable()
    {
        NetworkManager.OnInternetStateChanged += OnInternetChanged;
    }

    private void OnDisable()
    {
        NetworkManager.OnInternetStateChanged -= OnInternetChanged;
    }

    private void Start()
    {
        OnInternetChanged(NetworkManager.Instance.HasInternet);
    }

    private void OnInternetChanged(bool hasInternet)
    {
        _popup.SetActive(!hasInternet);
    }
}