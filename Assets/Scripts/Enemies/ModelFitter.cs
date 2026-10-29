using System.Collections;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Подгоняет модель под нужный рост по факту, а не по расчётным границам.
    ///
    /// У скелетных моделей границы в момент создания объекта ещё не пересчитаны,
    /// поэтому ждём первый отрисованный кадр — и только тогда меряем настоящие
    /// габариты, масштабируем и ставим ступни на ноль.
    /// </summary>
    [DisallowMultipleComponent]
    public class ModelFitter : MonoBehaviour
    {
        private Transform _host;
        private float _targetHeight;
        private bool _running;

        public void Configure(Transform host, float targetHeight)
        {
            _host = host;
            _targetHeight = Mathf.Max(0.1f, targetHeight);

            if (!_running && isActiveAndEnabled)
            {
                _running = true;
                StartCoroutine(Fit());
            }
        }

        private IEnumerator Fit()
        {
            // Два кадра: первый — чтобы объект появился в сцене,
            // второй — чтобы рендерер пересчитал границы.
            yield return null;
            yield return null;

            if (_host == null)
                yield break;

            if (TryMeasure(out var bottom, out var top))
            {
                var height = Mathf.Max(0.01f, top - bottom);
                transform.localScale *= _targetHeight / height;
            }

            // Ещё кадр — теперь масштаб применён, и можно поставить ступни на ноль.
            yield return null;

            if (_host != null && TryMeasure(out var newBottom, out _))
            {
                // Чуть вдавливаем в грунт: на песке и траве щель под ногами
                // читается как парение, даже если она в пару сантиметров.
                var shift = -newBottom - 0.04f;
                transform.position += _host.up * shift;
            }

            Destroy(this);
        }

        /// <summary>Нижняя и верхняя точки модели вдоль «вверх» объекта-хозяина.</summary>
        private bool TryMeasure(out float bottom, out float top)
        {
            bottom = float.MaxValue;
            top = float.MinValue;
            var found = false;
            var up = _host.up;
            var origin = _host.position;

            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer || !renderer.enabled)
                    continue;

                var bounds = renderer.bounds;
                found = true;

                for (var i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (i & 4) == 0 ? bounds.min.z : bounds.max.z);

                    var height = Vector3.Dot(corner - origin, up);
                    bottom = Mathf.Min(bottom, height);
                    top = Mathf.Max(top, height);
                }
            }

            return found && bottom <= top;
        }
    }
}
