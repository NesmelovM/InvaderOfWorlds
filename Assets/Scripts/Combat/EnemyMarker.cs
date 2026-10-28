using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Метка врага. Нужна, чтобы автооружие предпочитало живых противников,
    /// а не стены: и те и другие получают урон, но приоритет разный.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyMarker : MonoBehaviour
    {
    }
}
