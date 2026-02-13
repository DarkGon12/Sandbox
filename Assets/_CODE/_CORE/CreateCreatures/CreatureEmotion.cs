using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Creatures.Navigation;

namespace Creatures
{
    public class CreatureEmotion : MonoBehaviour
    {
        [Header("Emotion UI")]
        [SerializeField] private Canvas _emotionCanvas;
        [SerializeField] private Image _emotionImage;

        [Header("Level UI")]
        [SerializeField] private Canvas _levelCanvas;
        [SerializeField] private TMP_Text _levelText;

        [Header("Sprites")]
        [SerializeField] private Sprite _happyEmotionIcon;
        [SerializeField] private Sprite _angryEmotionIcon;

        [Header("Timing")]
        [SerializeField] private float _emotionIntervalMin = 5f;
        [SerializeField] private float _emotionIntervalMax = 10f;
        [SerializeField] private float _levelIntervalMin = 6f;
        [SerializeField] private float _levelIntervalMax = 12f;

        [Header("Animation")]
        [SerializeField] private float _emotionShowDuration = 1.2f;
        [SerializeField] private float _levelShowDuration = 1.0f;

        private CreatureStateView _stateView;
        private CreatureInformation _info;
        private Camera _mainCamera;

        private Tween _emotionTween;
        private Tween _levelTween;

        private float _emotionTimer;
        private float _levelTimer;

        private void Awake()
        {
            _stateView = GetComponentInParent<CreatureStateView>();
            _info = GetComponentInParent<CreatureInformation>();
            _mainCamera = Camera.main;

            _emotionCanvas.gameObject.SetActive(false);
            _levelCanvas.gameObject.SetActive(false);

            ResetEmotionTimer();
            ResetLevelTimer();
        }

        private void OnEnable()
        {
            _info.OnLevelUp += OnLevelChanged;
        }

        private void OnDisable()
        {
            _info.OnLevelUp -= OnLevelChanged;
        }

        private void Update()
        {
            HandleEmotions();
            HandleLevel();
        }

        private void LateUpdate()
        {
            RotateCanvasY(_emotionCanvas.transform);
            RotateCanvasY(_levelCanvas.transform);
        }

        // ===== ›ÃŒ÷»» =====

        private void HandleEmotions()
        {
            if (_stateView.GetSensor.HasEnemy)
            {
                ShowEmotion(_angryEmotionIcon);
                return;
            }

            _emotionTimer -= Time.deltaTime;
            if (_emotionTimer <= 0f)
            {
                ShowEmotion(_happyEmotionIcon);
                ResetEmotionTimer();
            }
        }

        private void ResetEmotionTimer()
        {
            _emotionTimer = Random.Range(_emotionIntervalMin, _emotionIntervalMax);
        }

        // ===== ”–Œ¬≈Õ‹ =====

        private void HandleLevel()
        {
            _levelTimer -= Time.deltaTime;
            if (_levelTimer <= 0f)
            {
                ShowLevel(_info.Level);
                ResetLevelTimer();
            }
        }

        private void ResetLevelTimer()
        {
            _levelTimer = Random.Range(_levelIntervalMin, _levelIntervalMax);
        }

        private void OnLevelChanged(int level)
        {
            ShowLevel(level);
            ShowEmotion(_happyEmotionIcon);
            ResetLevelTimer();
        }

        private void ShowLevel(int level)
        {
            _levelTween?.Kill();

            _levelText.text = $"LVL {level}";
            _levelCanvas.gameObject.SetActive(true);
            _levelCanvas.transform.localScale = Vector3.zero;

            _levelTween = _levelCanvas.transform
                .DOScale(1f, 0.25f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    _levelCanvas.transform
                        .DOScale(0f, 0.2f)
                        .SetDelay(_levelShowDuration)
                        .OnComplete(() =>
                        {
                            _levelCanvas.gameObject.SetActive(false);
                        });
                });
        }

        // ===== Œ“Œ¡–¿∆≈Õ»≈ ›ÃŒ÷»… =====

        private void ShowEmotion(Sprite sprite)
        {
            if (_emotionImage.sprite == sprite && _emotionCanvas.gameObject.activeSelf)
                return;

            _emotionTween?.Kill();

            _emotionImage.sprite = sprite;
            _emotionCanvas.gameObject.SetActive(true);
            _emotionCanvas.transform.localScale = Vector3.zero;

            _emotionTween = _emotionCanvas.transform
                .DOScale(1f, 0.25f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    _emotionCanvas.transform
                        .DOScale(0f, 0.2f)
                        .SetDelay(_emotionShowDuration)
                        .OnComplete(() =>
                        {
                            _emotionCanvas.gameObject.SetActive(false);
                        });
                });
        }

        // ===== œŒ¬Œ–Œ“ CANVAS “ŒÀ‹ Œ œŒ Y =====

        private void RotateCanvasY(Transform canvasTransform)
        {
            if (canvasTransform == null || _mainCamera == null)
                return;

            Vector3 direction = _mainCamera.transform.position - canvasTransform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion lookRotation = Quaternion.LookRotation(direction);
            Vector3 currentEuler = canvasTransform.rotation.eulerAngles;

            canvasTransform.rotation = Quaternion.Euler(
                currentEuler.x,
                lookRotation.eulerAngles.y,
                currentEuler.z
            );
        }
    }
}
