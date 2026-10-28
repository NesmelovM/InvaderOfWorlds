using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Описание биома: как выглядит планета и из каких префабов собирается.
    ///
    /// Это обычный ассет-скриптабл, поэтому новый биом добавляется без кода:
    /// создали объект, выбрали папку с префабами, задали цвета и место в каталоге.
    /// </summary>
    [CreateAssetMenu(fileName = "Biome", menuName = "Invader of Worlds/Биом")]
    public class BiomeDefinition : ScriptableObject
    {
        [Header("Название")]
        public string Title = "Новый биом";

        [Tooltip("Сколько порталов нужно пройти, чтобы биом открылся")]
        [Min(0)] public int RequiredPortals;

        [Header("Цвета")]
        public Color GroundColor = new Color(0.45f, 0.47f, 0.50f);
        public Color HillColor = new Color(0.42f, 0.38f, 0.34f);
        public Color SkyColor = new Color(0.02f, 0.03f, 0.08f);

        [Header("Плотность")]
        [Min(0)] public int BigProps = 90;
        [Min(0)] public int SmallProps = 600;

        [Header("Крупные объекты (преграждают путь)")]
        public List<GameObject> Big = new List<GameObject>();

        [Header("Мелочь (сквозь неё проходим)")]
        public List<GameObject> Small = new List<GameObject>();

        [Header("Убежища монстров")]
        [Tooltip("Из этих префабов собирается кольцо вокруг поселения. Пусто — обычные стены")]
        public List<GameObject> Walls = new List<GameObject>();

        [Tooltip("Сколько сегментов в кольце вокруг поселения")]
        [Min(3)] public int WallSegments = 6;

        [Tooltip("Высота сегмента стены в метрах")]
        [Min(1f)] public float WallHeight = 3f;

        [Header("Враги биома: по модели на тип")]
        [Tooltip("Мечник")]
        public GameObject EnemyModel1;
        [Tooltip("Лучник")]
        public GameObject EnemyModel2;
        [Tooltip("Элита")]
        public GameObject EnemyModel3;
        [Tooltip("Элитный лучник")]
        public GameObject EnemyModel4;
        [Tooltip("Босс")]
        public GameObject EnemyModel5;
        [Tooltip("Запасная модель: используется, если поле типа пустое")]
        public GameObject EnemyModel6;

        [Header("Модель с процедурной анимацией")]
        [Tooltip("Одна модель на всех врагов, анимация считается кодом — как у игрока")]
        public GameObject ProceduralModel;

        [Header("Оружие")]
        [Tooltip("Общий список: каждый персонаж и враг берёт случайную модель")]
        public List<GameObject> Weapons = new List<GameObject>();
    }
}
