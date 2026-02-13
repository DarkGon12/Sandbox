using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;

namespace Creatures.UI
{
    public class CreaturesWindow : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private RectTransform _spawnButton;
        [SerializeField] private RectTransform _spawnPanel;

        [Header("Animation")]
        [SerializeField] private float _duration = 0.4f;
        [SerializeField] private Ease _ease = Ease.OutBack;

        private Vector2 _panelTargetPos;
        private Vector3 _panelTargetScale;

        [SerializeField] private Button _creaturesShowWindowBtn;
        [SerializeField] private Button _creaturesHideWindowBtn;

        private void Awake()
        {
            _creaturesShowWindowBtn.onClick.AddListener(OpenPanel);
            _creaturesHideWindowBtn.onClick.AddListener(ClosePanel);

            _panelTargetPos = _spawnPanel.anchoredPosition;
            _panelTargetScale = _spawnPanel.localScale;

            _spawnPanel.localScale = Vector3.zero;
            _spawnPanel.gameObject.SetActive(false);
        }

        public void OpenPanel()
        {
            _spawnPanel.gameObject.SetActive(true);

            _spawnPanel.anchoredPosition = _spawnButton.anchoredPosition;
            _spawnPanel.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();

            seq.Join(_spawnPanel.DOScale(_panelTargetScale, _duration).SetEase(_ease));
            seq.Join(_spawnPanel.DOAnchorPos(_panelTargetPos, _duration).SetEase(Ease.OutCubic));
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
    }
}