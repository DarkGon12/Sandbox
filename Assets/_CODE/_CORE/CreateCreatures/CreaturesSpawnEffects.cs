using UnityEngine;
using DG.Tweening;

namespace Cretures
{
    public class CreaturesSpawnEffects : MonoBehaviour
    {
        [SerializeField] private float _spawnDuration = 0.5f;
        [SerializeField] private Ease _scaleEase = Ease.OutBack;
        [SerializeField] private Ease _fadeEase = Ease.OutQuad;

        [SerializeField] private float _startScale = 0f;
        [SerializeField] private float _endScale = 1f;

        [SerializeField] private Renderer[] _renderers;

        private MaterialPropertyBlock _propertyBlock;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            PlaySpawnEffect();
        }

        public void PlaySpawnEffect()
        {
            transform.localScale = Vector3.one * _startScale;
            transform.DOScale(_endScale, _spawnDuration)
                .SetEase(_scaleEase);

            foreach (Renderer renderer in _renderers)
            {
                renderer.GetPropertyBlock(_propertyBlock);
                Color color = renderer.sharedMaterial.color;
                color.a = 0f;
                _propertyBlock.SetColor(ColorProperty, color);
                renderer.SetPropertyBlock(_propertyBlock);

                DOTween.To(
                        () => 0f,
                        value =>
                        {
                            renderer.GetPropertyBlock(_propertyBlock);
                            color.a = value;
                            _propertyBlock.SetColor(ColorProperty, color);
                            renderer.SetPropertyBlock(_propertyBlock);
                        },
                        1f,
                        _spawnDuration
                    )
                    .SetEase(_fadeEase);
            }
        }
    }
}
