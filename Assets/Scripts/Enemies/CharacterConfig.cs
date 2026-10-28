using System;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>Облик одного типа врага: модель, оружие и рост.</summary>
    [Serializable]
    public class EnemyLook
    {
        public GameObject Model;
        public GameObject Weapon;
        [Min(0.5f)] public float Height = 1.8f;
        [Min(0.1f)] public float WeaponScale = 1f;
    }

    /// <summary>
    /// Облик персонажей и параметры процедурной анимации.
    ///
    /// Лежит в Resources, поэтому правки из окна «Персонажи» сразу попадают в игру.
    /// Пустое поле означает «как было в коде» — можно менять по одному типу.
    /// </summary>
    [CreateAssetMenu(fileName = "Config", menuName = "Invader of Worlds/Персонажи")]
    public class CharacterConfig : ScriptableObject
    {
        [Header("Враги")]
        public EnemyLook Melee = new EnemyLook { Height = 1.8f };
        public EnemyLook Ranged = new EnemyLook { Height = 1.9f };
        public EnemyLook Elite = new EnemyLook { Height = 2.5f };
        public EnemyLook EliteRanged = new EnemyLook { Height = 2.4f };
        public EnemyLook Boss = new EnemyLook { Height = 4.2f };

        [Header("Анимация")]
        [Min(0f)] public float StepSpeed = 8.5f;
        [Min(0f)] public float LegAmplitude = 32f;
        [Min(0f)] public float ArmAmplitude = 26f;
        [Min(0.05f)] public float AttackDuration = 0.28f;
    }

    /// <summary>Доступ к конфигу персонажей с безопасными значениями по умолчанию.</summary>
    public static class Characters
    {
        private const string ResourcePath = "Characters/Config";

        private static CharacterConfig _config;
        private static bool _loaded;

        public static CharacterConfig Config
        {
            get
            {
                if (!_loaded)
                {
                    _config = Resources.Load<CharacterConfig>(ResourcePath);
                    _loaded = true;
                }

                return _config;
            }
        }

        public static void ResetCache()
        {
            _config = null;
            _loaded = false;
        }

        public static EnemyLook Get(EnemyType type)
        {
            if (Config == null)
                return null;

            return type switch
            {
                EnemyType.Melee => Config.Melee,
                EnemyType.Ranged => Config.Ranged,
                EnemyType.Elite => Config.Elite,
                EnemyType.EliteRanged => Config.EliteRanged,
                EnemyType.Boss => Config.Boss,
                _ => null
            };
        }

        public static float StepSpeed => Config != null ? Config.StepSpeed : 8.5f;
        public static float LegAmplitude => Config != null ? Config.LegAmplitude : 32f;
        public static float ArmAmplitude => Config != null ? Config.ArmAmplitude : 26f;
        public static float AttackDuration => Config != null ? Config.AttackDuration : 0.28f;
    }

    public enum EnemyType
    {
        Melee,
        Ranged,
        Elite,
        EliteRanged,
        Boss
    }
}
