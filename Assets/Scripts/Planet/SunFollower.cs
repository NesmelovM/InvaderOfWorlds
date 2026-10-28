using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Держит направление солнца относительно игрока.
    ///
    /// Планета маленькая, а освещение в Unity задаётся мировым направлением.
    /// Из-за этого половина планеты всегда в тени: на обратной стороне игрок
    /// видит синий полусумрак. Здесь свет поворачивается вместе с игроком,
    /// поэтому освещение одинаковое в любой точке поверхности.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    public class SunFollower : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Planet _planet;
        [SerializeField] private Vector3 _angles = new Vector3(50f, -30f, 0f);

        private Light _light;

        public void Configure(Transform target, Planet planet)
        {
            _target = target;
            _planet = planet;
        }

        private void Awake()
        {
            _light = GetComponent<Light>();
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                var player = GameObject.FindWithTag("Player");
                _target = player != null ? player.transform : null;
            }

            var up = _target != null && _planet != null ? _planet.UpAt(_target.position) : Vector3.up;

            // Кадр строим от локального «вверх» игрока, а не через FromToRotation:
            // у противоположных векторов ось поворота не определена, и на южном
            // полюсе свет разворачивался так, что вся поверхность уходила в тень.
            var forward = Vector3.ProjectOnPlane(Vector3.forward, up);

            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.ProjectOnPlane(Vector3.right, up);

            var frame = Quaternion.LookRotation(forward.normalized, up);
            transform.rotation = frame * Quaternion.Euler(_angles);
        }
    }
}
