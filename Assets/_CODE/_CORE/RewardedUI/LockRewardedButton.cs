using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class LockRewardedButton : MonoBehaviour
{
    public bool Locked => _isRewardedUnlock;

    [SerializeField] private GameObject _adIcon;

    [Inject] private RewardedAdService _rewardedAdService;

    private bool _isRewardedUnlock;
    private Button _thisButton;

    private void Start()
    {
        _thisButton = GetComponent<Button>();
        _adIcon.SetActive(true);
        _thisButton.onClick.AddListener(WatchRewardedAd);
        _isRewardedUnlock = false;
    }

    private void WatchRewardedAd()
    {
        _rewardedAdService.ShowAd(OnRewaded);
    }

    private void OnRewaded()
    {
        _thisButton.onClick.RemoveListener(WatchRewardedAd);
        _adIcon.SetActive(false);
        _isRewardedUnlock = true;
    }
}