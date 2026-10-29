using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Ручные правки уже сгенерированной планеты: выровнять врагов по грунту,
    /// убрать зависших, перенести портал из постройки.
    ///
    /// Всё это работает в Play Mode — мир создаётся в рантайме, поэтому править
    /// его можно только когда он существует. Инструмент разработчика вызывает
    /// эти методы кнопками.
    /// </summary>
    public static class WorldFix
    {
        /// <summary>Враг и насколько его ступни выше или ниже грунта.</summary>
        public struct EnemyInfo
        {
            public EnemyMarker Enemy;
            public string Name;
            public float FeetOffset;
            public float Height;
        }

        public static Planet FindPlanet()
        {
            return Object.FindAnyObjectByType<Planet>();
        }

        public static List<EnemyInfo> Inspect(bool includeAligned = true)
        {
            var result = new List<EnemyInfo>();
            var planet = FindPlanet();

            if (planet == null)
                return result;

            foreach (var enemy in Object.FindObjectsByType<EnemyMarker>(FindObjectsSortMode.None))
            {
                if (enemy == null)
                    continue;

                var info = new EnemyInfo
                {
                    Enemy = enemy,
                    Name = enemy.name,
                    FeetOffset = MeasureFeetOffset(enemy, planet, out var height),
                    Height = height
                };

                if (!includeAligned && Mathf.Abs(info.FeetOffset) < 0.05f)
                    continue;

                result.Add(info);
            }

            result.Sort((a, b) => Mathf.Abs(b.FeetOffset).CompareTo(Mathf.Abs(a.FeetOffset)));
            return result;
        }

        /// <summary>Выравнивает всех врагов по грунту. Возвращает, скольких поправили.</summary>
        public static int AlignAllToGround(float tolerance = 0.03f)
        {
            var planet = FindPlanet();

            if (planet == null)
                return 0;

            var fixedCount = 0;

            foreach (var enemy in Object.FindObjectsByType<EnemyMarker>(FindObjectsSortMode.None))
            {
                if (enemy == null)
                    continue;

                var offset = MeasureFeetOffset(enemy, planet, out _);

                if (Mathf.Abs(offset) <= tolerance)
                    continue;

                var up = planet.UpAt(enemy.transform.position);
                enemy.transform.position -= up * offset;
                fixedCount++;
            }

            return fixedCount;
        }

        /// <summary>Выравнивает одного врага. Возвращает true, если поправили.</summary>
        public static bool AlignToGround(EnemyMarker enemy)
        {
            var planet = FindPlanet();

            if (planet == null || enemy == null)
                return false;

            var offset = MeasureFeetOffset(enemy, planet, out _);

            if (Mathf.Abs(offset) < 0.01f)
                return false;

            var up = planet.UpAt(enemy.transform.position);
            enemy.transform.position -= up * offset;
            return true;
        }

        /// <summary>Убирает зависших: сажает на грунт, а если место занято — удаляет.</summary>
        public static int ClearFloating(float threshold = 0.25f)
        {
            var planet = FindPlanet();

            if (planet == null)
                return 0;

            var removed = 0;

            foreach (var enemy in Object.FindObjectsByType<EnemyMarker>(FindObjectsSortMode.None))
            {
                if (enemy == null)
                    continue;

                if (Mathf.Abs(MeasureFeetOffset(enemy, planet, out _)) < threshold)
                    continue;

                Object.Destroy(enemy.gameObject);
                removed++;
            }

            return removed;
        }

        /// <summary>Переносит портал на ближайшее свободное место.</summary>
        public static bool MovePortalToFreeSpot(out Vector3 position)
        {
            position = Vector3.zero;
            var planet = FindPlanet();
            var portal = Object.FindAnyObjectByType<ExitPortal>();

            if (planet == null || portal == null)
                return false;

            if (!SpawnPlacement.TryFindFreeGroundPosition(planet, portal.transform.position, 14f, 40,
                    out var free, out var up, 3.5f))
                return false;

            portal.transform.position = free + up * 0.12f;
            portal.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
            position = portal.transform.position;
            return true;
        }

        /// <summary>Насколько ступни врага выше грунта: плюс — висит, минус — утонул.</summary>
        private static float MeasureFeetOffset(EnemyMarker enemy, Planet planet, out float height)
        {
            height = 0f;
            var model = enemy.transform.Find("Model");

            if (model == null)
                return 0f;

            var up = planet.UpAt(enemy.transform.position);
            var origin = enemy.transform.position;
            var ground = PlanetSurface.SampleGroundRadius(planet, up, 40f, ~0, enemy.transform, out _);
            var groundPoint = planet.Center + up * ground;

            var bottom = float.MaxValue;
            var top = float.MinValue;

            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer || !renderer.enabled)
                    continue;

                var name = renderer.transform.parent != null ? renderer.transform.parent.name : "";

                if (name.StartsWith("Weapon"))
                    continue;

                var bounds = renderer.bounds;

                for (var i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (i & 4) == 0 ? bounds.min.z : bounds.max.z);

                    var alongUp = Vector3.Dot(corner - groundPoint, up);
                    bottom = Mathf.Min(bottom, alongUp);
                    top = Mathf.Max(top, alongUp);
                }
            }

            if (bottom >= float.MaxValue)
                return 0f;

            height = top - bottom;
            return bottom;
        }
    }
}
