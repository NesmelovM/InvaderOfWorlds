using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Собирает модель игрока из конфига персонажей при старте.
    ///
    /// Модель в сцене — запасной вариант. Если в конфиге указана другая, она
    /// пересобирается здесь, поэтому менять внешний вид игрока можно в окне
    /// «Персонажи» без пересборки сцены.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public class PlayerAppearance : MonoBehaviour
    {
        private GameObject _model;

        private void Awake()
        {
            var look = Characters.PlayerLook;

            if (look == null || look.Model == null)
                return;

            // Убираем модель из сцены, чтобы не осталось двух.
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child == transform || child.name != "Model")
                    continue;

                Destroy(child.gameObject);
                break;
            }

            _model = CharacterModel.Attach(gameObject, look.Model, look.Height, 0f);

            if (_model == null)
                return;

            var tint = GetComponent<ModelTint>();

            if (tint == null)
                tint = gameObject.AddComponent<ModelTint>();

            tint.Configure(Characters.PlayerTint);
            tint.Apply();
        }
    }
}
