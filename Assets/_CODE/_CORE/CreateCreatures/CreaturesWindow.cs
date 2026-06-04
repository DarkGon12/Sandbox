using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;
using System;
using Zenject;
using MessagePipe;
using System.Collections;

namespace Creatures.UI
{
    public class CreaturesWindow : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private RectTransform _spawnButton;
        [SerializeField] private RectTransform _spawnPanel;
        [SerializeField] private CreaturePanelTutorial _arrowTutorial;

        [Header("Animation")]
        [SerializeField] private float _duration = 0.4f;
        [SerializeField] private Ease _ease = Ease.OutBack;
        [SerializeField] private float _showDelay;

        private bool _firstTap = true;
        private Vector2 _panelTargetPos;
        private Vector3 _panelTargetScale;

        [SerializeField] private WeaponHolderSwitcher _weaponHolderSwitcher;

        [SerializeField] private Button _creaturesShowWindowBtn;
        [SerializeField] private Button _creaturesHideWindowBtn;

        [Inject] private ISubscriber<CreatureWindowView> _showWindowEventSub;

        private IDisposable _disposable;

        private void Awake()
        {
            _creaturesShowWindowBtn.onClick.AddListener(OpenPanel);
            _creaturesHideWindowBtn.onClick.AddListener(ClosePanel);

            var bag = DisposableBag.CreateBuilder();
            _showWindowEventSub.Subscribe(ShowWindowEvent).AddTo(bag);
            _disposable = bag.Build();

            _panelTargetPos = _spawnPanel.anchoredPosition;
            _panelTargetScale = _spawnPanel.localScale;

            _spawnPanel.localScale = Vector3.zero;
            _spawnPanel.gameObject.SetActive(false);
        }

        public void ShowWindowEvent(CreatureWindowView evt)
        {
            if (evt.IsShow)
                OpenPanel();
            else
                ClosePanel();
        }

        public void OpenPanel()
        {
            StartCoroutine(ShowDelay());
        }

        public void ClosePanel()
        {
            Sequence seq = DOTween.Sequence();

            seq.Join(_spawnPanel.DOScale(Vector3.zero, _duration * 0.8f).SetEase(Ease.InBack));
            seq.Join(_spawnPanel.DOAnchorPos(_spawnButton.anchoredPosition, _duration * 0.8f));

            seq.OnComplete(() =>
            {
                _spawnPanel.gameObject.SetActive(false);
            });
        }

        private void OnDestroy() => _disposable?.Dispose();

        private IEnumerator ShowDelay()
        {
            _weaponHolderSwitcher.ShowHeld(true);
            yield return new WaitForSeconds(_showDelay);

            if (_firstTap)
            {
                _firstTap = false;
                _arrowTutorial.NextStep();
            }

            _spawnPanel.gameObject.SetActive(true);

            _spawnPanel.anchoredPosition = _spawnButton.anchoredPosition;
            _spawnPanel.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();

            seq.Join(_spawnPanel.DOScale(_panelTargetScale, _duration).SetEase(_ease));
            seq.Join(_spawnPanel.DOAnchorPos(_panelTargetPos, _duration).SetEase(Ease.OutCubic));
            
            _weaponHolderSwitcher.ShowHeld(false);
            _weaponHolderSwitcher.SetWeapon(0);
        }
    }
}