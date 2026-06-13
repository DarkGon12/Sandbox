using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sandbox.MapEditor
{
    public sealed class MapEditorToolbarUIController : MonoBehaviour
    {
        [Serializable]
        private sealed class PanelButtonBinding
        {
            [SerializeField] private Button _button;
            [SerializeField] private RectTransform _window;

            public Button Button => _button;
            public RectTransform Window => _window;
        }

        private sealed class PanelAnimationState
        {
            public Vector2 OpenPosition;
            public Vector3 OpenScale;
            public Sequence Sequence;
            public CanvasGroup CanvasGroup;
        }

        [Header("Open")]
        [SerializeField] private PanelButtonBinding[] _topPanelButtons;

        [Header("Close")]
        [SerializeField] private PanelButtonBinding[] _closeButtons;

        [Header("Animation")]
        [SerializeField] private float _duration = 0.35f;
        [SerializeField] private Ease _openEase = Ease.OutBack;
        [SerializeField] private Ease _closeEase = Ease.InBack;
        [SerializeField] private bool _closeOtherWindows = true;
        [SerializeField] private bool _hideWindowsOnStart = true;
        [SerializeField] private bool _animateFade = true;
        [SerializeField] private bool _useUnscaledTime;

        private readonly Dictionary<RectTransform, PanelAnimationState> _states = new();
        private readonly Dictionary<Button, UnityAction> _registeredActions = new();

        private void Awake()
        {
            CacheWindows(_topPanelButtons);
            CacheWindows(_closeButtons);

            if (!_hideWindowsOnStart)
                return;

            foreach (RectTransform window in _states.Keys)
                HideWindowInstant(window);
        }

        private void OnEnable()
        {
            RegisterButtons(_topPanelButtons, OpenWindow);
            RegisterButtons(_closeButtons, CloseWindow);
        }

        private void OnDisable()
        {
            UnregisterButtons();
            KillAllSequences();
        }

        public void OpenByIndex(int index)
        {
            if (_topPanelButtons == null || index < 0 || index >= _topPanelButtons.Length)
                return;

            OpenWindow(_topPanelButtons[index]);
        }

        public void CloseByIndex(int index)
        {
            if (_closeButtons == null || index < 0 || index >= _closeButtons.Length)
                return;

            CloseWindow(_closeButtons[index]);
        }

        private void RegisterButtons(PanelButtonBinding[] bindings, Action<PanelButtonBinding> clickAction)
        {
            if (bindings == null)
                return;

            foreach (PanelButtonBinding binding in bindings)
            {
                if (!IsBindingValid(binding) || _registeredActions.ContainsKey(binding.Button))
                    continue;

                PanelButtonBinding capturedBinding = binding;
                UnityAction action = () => clickAction(capturedBinding);
                binding.Button.onClick.AddListener(action);
                _registeredActions.Add(binding.Button, action);
            }
        }

        private void UnregisterButtons()
        {
            foreach (KeyValuePair<Button, UnityAction> action in _registeredActions)
            {
                if (action.Key != null)
                    action.Key.onClick.RemoveListener(action.Value);
            }

            _registeredActions.Clear();
        }

        private void OpenWindow(PanelButtonBinding binding)
        {
            if (!IsBindingValid(binding))
                return;

            RectTransform window = binding.Window;
            PanelAnimationState state = GetState(window);

            if (_closeOtherWindows)
                CloseOtherWindows(window);

            state.Sequence?.Kill();

            window.gameObject.SetActive(true);
            window.SetAsLastSibling();
            window.anchoredPosition = GetButtonPositionInWindowParent(binding.Button, window, state.OpenPosition);
            window.localScale = Vector3.zero;

            SetupCanvasGroup(state, blocksRaycasts: false, interactable: false);
            SetCanvasGroupAlpha(state, 0f);

            state.Sequence = DOTween.Sequence().SetUpdate(_useUnscaledTime);
            state.Sequence.Join(window.DOScale(state.OpenScale, _duration).SetEase(_openEase));
            state.Sequence.Join(window.DOAnchorPos(state.OpenPosition, _duration).SetEase(Ease.OutCubic));

            if (_animateFade && state.CanvasGroup != null)
                state.Sequence.Join(state.CanvasGroup.DOFade(1f, _duration * 0.85f).SetEase(Ease.OutSine));

            state.Sequence.OnComplete(() =>
            {
                SetupCanvasGroup(state, blocksRaycasts: true, interactable: true);
                state.Sequence = null;
            });
        }

        private void CloseWindow(PanelButtonBinding binding)
        {
            if (!IsBindingValid(binding))
                return;

            CloseWindow(binding.Window, binding.Button);
        }

        private void CloseWindow(RectTransform window, Button targetButton)
        {
            if (window == null || !_states.ContainsKey(window))
                return;

            PanelAnimationState state = GetState(window);
            state.Sequence?.Kill();

            SetupCanvasGroup(state, blocksRaycasts: false, interactable: false);

            Vector2 targetPosition = GetButtonPositionInWindowParent(targetButton, window, state.OpenPosition);
            state.Sequence = DOTween.Sequence().SetUpdate(_useUnscaledTime);
            state.Sequence.Join(window.DOScale(Vector3.zero, _duration * 0.8f).SetEase(_closeEase));
            state.Sequence.Join(window.DOAnchorPos(targetPosition, _duration * 0.8f).SetEase(Ease.InCubic));

            if (_animateFade && state.CanvasGroup != null)
                state.Sequence.Join(state.CanvasGroup.DOFade(0f, _duration * 0.65f).SetEase(Ease.InSine));

            state.Sequence.OnComplete(() =>
            {
                window.gameObject.SetActive(false);
                window.anchoredPosition = state.OpenPosition;
                window.localScale = state.OpenScale;
                SetCanvasGroupAlpha(state, 1f);
                SetupCanvasGroup(state, blocksRaycasts: true, interactable: true);
                state.Sequence = null;
            });
        }

        private void CloseOtherWindows(RectTransform activeWindow)
        {
            foreach (RectTransform window in _states.Keys)
            {
                if (window == null || window == activeWindow || !window.gameObject.activeSelf)
                    continue;

                CloseWindow(window, null);
            }
        }

        private void CacheWindows(PanelButtonBinding[] bindings)
        {
            if (bindings == null)
                return;

            foreach (PanelButtonBinding binding in bindings)
            {
                if (binding == null || binding.Window == null || _states.ContainsKey(binding.Window))
                    continue;

                Vector3 startScale = binding.Window.localScale;
                if (startScale == Vector3.zero)
                    startScale = Vector3.one;

                _states.Add(binding.Window, new PanelAnimationState
                {
                    OpenPosition = binding.Window.anchoredPosition,
                    OpenScale = startScale,
                    CanvasGroup = GetOrAddCanvasGroup(binding.Window)
                });
            }
        }

        private PanelAnimationState GetState(RectTransform window)
        {
            if (_states.TryGetValue(window, out PanelAnimationState state))
                return state;

            Vector3 startScale = window.localScale;
            if (startScale == Vector3.zero)
                startScale = Vector3.one;

            state = new PanelAnimationState
            {
                OpenPosition = window.anchoredPosition,
                OpenScale = startScale,
                CanvasGroup = GetOrAddCanvasGroup(window)
            };

            _states.Add(window, state);
            return state;
        }

        private CanvasGroup GetOrAddCanvasGroup(RectTransform window)
        {
            if (!_animateFade || window == null)
                return null;

            if (window.TryGetComponent(out CanvasGroup canvasGroup))
                return canvasGroup;

            return window.gameObject.AddComponent<CanvasGroup>();
        }

        private void SetupCanvasGroup(PanelAnimationState state, bool blocksRaycasts, bool interactable)
        {
            if (state.CanvasGroup == null)
                return;

            state.CanvasGroup.blocksRaycasts = blocksRaycasts;
            state.CanvasGroup.interactable = interactable;
        }

        private void HideWindowInstant(RectTransform window)
        {
            PanelAnimationState state = GetState(window);
            state.Sequence?.Kill();

            window.anchoredPosition = state.OpenPosition;
            window.localScale = state.OpenScale;

            if (state.CanvasGroup != null)
                state.CanvasGroup.alpha = 1f;

            window.gameObject.SetActive(false);
        }

        private void SetCanvasGroupAlpha(PanelAnimationState state, float alpha)
        {
            if (state.CanvasGroup != null)
                state.CanvasGroup.alpha = alpha;
        }

        private Vector2 GetButtonPositionInWindowParent(Button button, RectTransform window, Vector2 fallback)
        {
            if (button == null || window == null || window.parent == null)
                return fallback;

            RectTransform buttonRect = button.transform as RectTransform;
            RectTransform windowParent = window.parent as RectTransform;

            if (buttonRect == null || windowParent == null)
                return fallback;

            Vector3 worldPoint = buttonRect.TransformPoint(buttonRect.rect.center);
            Vector3 localPoint = windowParent.InverseTransformPoint(worldPoint);
            return new Vector2(localPoint.x, localPoint.y);
        }

        private bool IsBindingValid(PanelButtonBinding binding)
        {
            return binding != null && binding.Button != null && binding.Window != null;
        }

        private void KillAllSequences()
        {
            foreach (PanelAnimationState state in _states.Values)
                state.Sequence?.Kill();
        }
    }
}
