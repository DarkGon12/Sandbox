using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class CheatPanels : MonoBehaviour
{
    [SerializeField] private GameObject _cheatPanel;
    [SerializeField] private Button _openCheatPanelBtn;

    [SerializeField] private SceneAdsController _adController;
    [Inject] private IPublisher<CreatureVisibleEffect> _creatureVisibleEffectEventPub;

    private bool _isOpen;

    private void Start()
    {
        _openCheatPanelBtn.onClick.AddListener(ShowPanel);
    }

    private void ShowPanel()
    {
        _isOpen = !_isOpen;

        if (_isOpen)
            _cheatPanel.SetActive(true);
        else
            _cheatPanel.SetActive(false);
    }

    public void AdsVisible(bool state)
    {
        _adController.GetBanner.ShowBanner(state);
        _adController.CanVisibleInter(state);
        _adController.GetAppOpen.CanShow = state;
    }

    public void BloodEffectVisible(bool state)
    {
        _creatureVisibleEffectEventPub.Publish(new() { IsShow = state });
    }
}