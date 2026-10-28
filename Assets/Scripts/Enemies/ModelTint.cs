using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Красит модель в заданный цвет при запуске.
    ///
    /// Материал, созданный в редакторе через EnemyFactory, не сохраняется в сцене:
    /// это не ассет, поэтому после перезагрузки модель становится белой. Компонент
    /// с цветом переживает сохранение, а материал создаётся уже в игре.
    /// </summary>
    [DisallowMultipleComponent]
    public class ModelTint : MonoBehaviour
    {
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private bool _emission;

        public void Configure(Color color, bool emission = false)
        {
            _color = color;
            _emission = emission;
        }

        private void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            var material = EnemyFactory.GetMaterial(_color);

            if (_emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", _color * 0.7f);
            }

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.GetComponent<ParticleSystemRenderer>() != null)
                    continue;

                renderer.sharedMaterial = material;
            }
        }
    }
}
