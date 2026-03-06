using System.Collections;
using UnityEngine;

namespace Resonance.Environment
{
    [RequireComponent(typeof(MeshRenderer))]
    public class GlassShard : MonoBehaviour
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private const float ShardSpawnAlpha = 0.7f;

        private Material _instancedMaterial;
        private bool _useBaseColor;
        private bool _hasImpacted;

        private AK.Wwise.Event _impactEvent;
        private float _minImpactSpeed;

        public void Initialize(float fadeDelay, float fadeDuration, AK.Wwise.Event impactEvent, float minImpactSpeed)
        {
            _impactEvent    = impactEvent;
            _minImpactSpeed = minImpactSpeed;

            _instancedMaterial = GetComponent<MeshRenderer>().material;
            _useBaseColor      = _instancedMaterial.HasProperty(BaseColorProperty);

            Color c = _useBaseColor
                ? _instancedMaterial.GetColor(BaseColorProperty)
                : _instancedMaterial.color;
            c.a = ShardSpawnAlpha;
            if (_useBaseColor) _instancedMaterial.SetColor(BaseColorProperty, c);
            else               _instancedMaterial.color = c;

            StartCoroutine(FadeAndDestroy(fadeDelay, fadeDuration));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasImpacted) return;
            if (collision.relativeVelocity.magnitude < _minImpactSpeed) return;

            _hasImpacted = true;
            _impactEvent.Post(gameObject);
        }

        private IEnumerator FadeAndDestroy(float fadeDelay, float fadeDuration)
        {
            yield return new WaitForSeconds(fadeDelay);

            Color startColor = _useBaseColor
                ? _instancedMaterial.GetColor(BaseColorProperty)
                : _instancedMaterial.color;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                Color c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, elapsed / fadeDuration);

                if (_useBaseColor) _instancedMaterial.SetColor(BaseColorProperty, c);
                else               _instancedMaterial.color = c;

                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_instancedMaterial != null)
                Destroy(_instancedMaterial);
        }
    }
}