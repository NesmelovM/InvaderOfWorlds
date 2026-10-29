using System.Collections;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Ставит модель ступнями на ноль хозяина и подгоняет под неё капсулу.
    ///
    /// Модель не масштабируется: у неё остаётся тот размер, в котором её сделали,
    /// а хитбокс подстраивается под силуэт. Границы скелетной модели готовы
    /// только после первого отрисованного кадра, поэтому замер отложен.
    /// </summary>
    [DisallowMultipleComponent]
    public class ModelFitter : MonoBehaviour
    {
        private Transform _host;
        private CapsuleCollider _body;
        private Renderer[] _renderers;
        private bool _running;
        private float _targetHeight;

        public void Configure(Transform host, CapsuleCollider body = null, float targetHeight = 0f)
        {
            _host = host;
            _body = body;
            _targetHeight = targetHeight;

            // Запоминаем рендереры сразу: оружие вешается в руку уже после,
            // и если мерить вместе с ним, габариты получаются неверными.
            _renderers = GetComponentsInChildren<Renderer>();

            if (!_running && isActiveAndEnabled)
            {
                _running = true;
                StartCoroutine(Fit());
            }
        }

        private IEnumerator Fit()
        {
            // Три кадра: объект появляется, рендерер пересчитывает границы,
            // а скелет успевает встать в позу покоя.
            yield return null;
            yield return null;
            yield return null;

            if (_host == null)
                yield break;

            if (TryMeasure(out var bottom, out var top, out var halfWidth))
            {
                // Подгонка роста — только по запросу из конфига персонажей.
                if (_targetHeight > 0.1f)
                {
                    var current = Mathf.Max(0.01f, top - bottom);
                    transform.localScale *= _targetHeight / current;
                    yield return null;

                    if (_host == null)
                        yield break;

                    TryMeasure(out bottom, out top, out halfWidth);
                }

                transform.position += _host.up * (-bottom - 0.02f);

                if (_body != null)
                {
                    var height = Mathf.Max(0.6f, top - bottom);
                    _body.height = height;
                    _body.center = new Vector3(0f, height * 0.5f, 0f);

                    // Радиус — по фактической ширине, но не больше прежнего:
                    // слишком толстая капсула мешает проходить двери.
                    _body.radius = Mathf.Clamp(halfWidth * 0.8f, 0.2f, _body.radius);
                }
            }

            Destroy(this);
        }

        /// <summary>Оружие и прочие навески в руке не входят в рост персонажа.</summary>
        private bool IsPartOfAttachment(Transform target)
        {
            for (var current = target; current != null && current != transform; current = current.parent)
            {
                var name = current.name;

                if (name.StartsWith("Weapon") || name.Contains("Weapon"))
                    return true;
            }

            return false;
        }

        /// <summary>Габариты модели вдоль «вверх» хозяина и половина ширины.</summary>
        private bool TryMeasure(out float bottom, out float top, out float halfWidth)
        {
            bottom = float.MaxValue;
            top = float.MinValue;
            halfWidth = 0.3f;
            var found = false;
            var up = _host.up;
            var origin = _host.position;
            var maxSide = 0f;

            foreach (var renderer in _renderers)
            {
                if (renderer == null || renderer is ParticleSystemRenderer || !renderer.enabled)
                    continue;

                if (IsPartOfAttachment(renderer.transform))
                    continue;

                var bounds = renderer.bounds;
                found = true;

                var size = bounds.size;
                maxSide = Mathf.Max(maxSide, Mathf.Max(size.x, size.z));

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

            halfWidth = maxSide * 0.5f;
            return found && bottom <= top;
        }
    }
}
