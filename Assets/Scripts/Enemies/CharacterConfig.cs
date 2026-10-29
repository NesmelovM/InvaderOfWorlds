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
        [Header("Перекраска")]
        [Tooltip("Выключено: у моделей остаются их собственные материалы")]
        public bool Colorize;

        [Header("Игрок")]
        public EnemyLook Player = new EnemyLook { Height = 1.95f };
        public Color PlayerTint = new Color(0.16f, 0.07f, 0.23f);

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

        /// <summary>
        /// Облик врага: модель берётся из биома (одна на все типы), рост — по типу.
        /// Если в биоме пусто, используется общий конфиг, а потом встроенные модели.
        /// </summary>
        public static EnemyLook Get(EnemyType type)
        {
            var biome = RunState.ActiveBiome;

            if (biome != null)
            {
                var model = TypeModel(biome, type) ?? biome.EnemyModel6 ?? biome.ProceduralModel;

                if (model != null)
                    return new EnemyLook { Model = model, Height = DefaultHeight(type) };
            }

            if (Config == null)
                return null;

            return Pick(Config, type);
        }

        /// <summary>Модель конкретного типа врага в биоме.</summary>
        public static GameObject TypeModel(BiomeDefinition biome, EnemyType type)
        {
            return type switch
            {
                EnemyType.Melee => biome.EnemyModel1,
                EnemyType.Ranged => biome.EnemyModel2,
                EnemyType.Elite => biome.EnemyModel3,
                EnemyType.EliteRanged => biome.EnemyModel4,
                EnemyType.Boss => biome.EnemyModel5,
                _ => biome.EnemyModel6
            };
        }

        /// <summary>Случайное оружие биома: для стрелков — из дальнего списка, иначе из ближнего.</summary>
        public static GameObject RandomWeapon(bool ranged)
        {
            var biome = RunState.ActiveBiome;

            if (biome == null)
                return null;

            var list = ranged ? biome.RangedWeapons : biome.MeleeWeapons;

            if (list == null || list.Count == 0)
                return null;

            var valid = 0;

            foreach (var weapon in list)
            {
                if (weapon != null)
                    valid++;
            }

            if (valid == 0)
                return null;

            var pick = UnityEngine.Random.Range(0, valid);

            foreach (var weapon in list)
            {
                if (weapon == null)
                    continue;

                if (pick-- == 0)
                    return weapon;
            }

            return null;
        }
        public static float DefaultHeight(EnemyType type)
        {
            return type switch
            {
                EnemyType.Melee => 1.8f,
                EnemyType.Ranged => 1.9f,
                EnemyType.Elite => 2.5f,
                EnemyType.EliteRanged => 2.4f,
                EnemyType.Boss => 4.2f,
                _ => 1.8f
            };
        }
        public static EnemyLook Pick(CharacterConfig config, EnemyType type)
        {
            if (config == null)
                return null;

            return type switch
            {
                EnemyType.Melee => config.Melee,
                EnemyType.Ranged => config.Ranged,
                EnemyType.Elite => config.Elite,
                EnemyType.EliteRanged => config.EliteRanged,
                EnemyType.Boss => config.Boss,
                _ => null
            };
        }

        public static bool Colorize => Config != null && Config.Colorize;

        public static EnemyLook PlayerLook => Config != null ? Config.Player : null;
        public static Color PlayerTint => Config != null ? Config.PlayerTint : new Color(0.16f, 0.07f, 0.23f);

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
