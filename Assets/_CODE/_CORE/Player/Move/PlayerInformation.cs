using DG.Tweening;
using MessagePipe;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace Sandbox.Player
{
    public class PlayerInformation : MonoBehaviour
    {
        [Header("UI Effects")]
        [SerializeField] private Image _deadEffect;
        [SerializeField] private Image _bloodEffect;

        [Header("Health")]
        [SerializeField] private int _playerHealth = 200;
        [SerializeField] private TextMeshProUGUI _playerHealth_Text;

        [Header("Timings")]
        [SerializeField] private float _bloodFadeIn = 0.15f;
        [SerializeField] private float _bloodFadeOut = 0.6f;
        [SerializeField] private float _deadFadeIn = 1.2f;

        [Inject] private IPublisher<LoadSceneEvent> _loadSceneEventPub;

        private string _currentScene;
        private bool _isDead;

        private Tween _bloodTween;
        private Tween _deadTween;

        private void Start()
        {
            _currentScene = SceneManager.GetActiveScene().name;

            SetImageAlpha(_bloodEffect, 0f);
            SetImageAlpha(_deadEffect, 0f);
        }

        public void TakeDamage(int damage)
        {
            if (_isDead)
                return;

            _playerHealth -= damage;
            _playerHealth_Text.text = _playerHealth.ToString();

            PlayBloodEffect();

            if (_playerHealth <= 0)
            {
                _isDead = true;
                StartCoroutine(WaitUntilDead());
            }
        }

        private void PlayBloodEffect()
        {
            _bloodTween?.Kill();

            _bloodEffect.gameObject.SetActive(true);
            SetImageAlpha(_bloodEffect, 0f);

            _bloodTween = _bloodEffect
                .DOFade(0.8f, _bloodFadeIn)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _bloodEffect
                        .DOFade(0f, _bloodFadeOut)
                        .SetEase(Ease.InQuad);
                });
        }

        private IEnumerator WaitUntilDead()
        {
            PlayDeadEffect();
            yield return new WaitForSeconds(4f);
        }

        private void PlayDeadEffect()
        {
            _deadTween?.Kill();

            _deadEffect.gameObject.SetActive(true);
            SetImageAlpha(_deadEffect, 0f);

            _deadTween = _deadEffect
                .DOFade(1f, _deadFadeIn)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    Invoke(nameof(ReloadScene), 1f);
                });
        }

        private void ReloadScene()
        {
            _loadSceneEventPub.Publish(new() { SceneName = _currentScene });
        }

        private void SetImageAlpha(Image img, float a)
        {
            Color c = img.color;
            c.a = a;
            img.color = c;
        }
    }
}