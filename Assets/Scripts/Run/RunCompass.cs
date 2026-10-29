using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Компас забега: полоса сверху показывает, где портал на следующую планету
    /// и где сундуки.
    ///
    /// Направление считается относительно взгляда игрока и проецируется на шкалу,
    /// поэтому искать цель по огромной планете больше не нужно на глаз.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunCompass : MonoBehaviour
    {
        private const float VisibleAngle = 80f;
        private const float BarWidth = 760f;
        private const float BarHeight = 34f;
        private const float BarTop = 104f;

        private static readonly Color PortalColor = new Color(0.35f, 0.85f, 1f);
        private static readonly Color ChestColor = new Color(1f, 0.82f, 0.30f);

        private readonly List<Chest> _chests = new List<Chest>();
        private Transform _player;
        private Planet _planet;
        private GUIStyle _distanceStyle;

        private void Awake()
        {
            var player = GameObject.FindWithTag("Player");

            if (player != null)
                _player = player.transform;

            _planet = FindAnyObjectByType<Planet>();
        }

        private void OnGUI()
        {
            if (_player == null || _planet == null)
                return;

            if (UiState.BlocksInput)
                return;

            EnsureStyles();
            UiCanvas.Begin();

            var up = (_player.position - _planet.Center).normalized;
            var forward = Vector3.ProjectOnPlane(_player.forward, up);

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.ProjectOnPlane(Vector3.forward, up);

            forward.Normalize();

            var bar = new Rect((UiCanvas.Width - BarWidth) * 0.5f, BarTop, BarWidth, BarHeight);
            HealthBarDrawer.DrawRect(bar, new Color(0.03f, 0.04f, 0.07f, 0.62f));

            // Деления шкалы: север относительно взгляда игрока.
            for (var degrees = -VisibleAngle; degrees <= VisibleAngle; degrees += 20f)
            {
                var x = bar.x + BarWidth * 0.5f + degrees / VisibleAngle * BarWidth * 0.5f;
                var height = Mathf.Abs(degrees) < 1f ? 14f : 8f;
                HealthBarDrawer.DrawRect(new Rect(x - 1f, bar.y + BarHeight - height - 4f, 2f, height),
                    new Color(0.7f, 0.75f, 0.85f, 0.5f));
            }

            DrawTargets(bar, up, forward);

            // Стрелка центра.
            HealthBarDrawer.DrawRect(new Rect(bar.x + BarWidth * 0.5f - 2f, bar.y - 6f, 4f, 10f),
                new Color(0.95f, 0.85f, 0.55f, 0.9f));

            UiCanvas.End();
        }

        private void DrawTargets(Rect bar, Vector3 up, Vector3 forward)
        {
            var portal = FindAnyObjectByType<ExitPortal>();

            if (portal != null)
            {
                var toPortal = portal.transform.position - _player.position;
                var distance = toPortal.magnitude;
                DrawMarker(bar, up, forward, toPortal, PortalColor, 10f);

                if (TryGetOffset(bar, up, forward, toPortal, out var x))
                {
                    GUI.Label(new Rect(x - 90f, bar.y + BarHeight + 2f, 180f, 22f),
                        Mathf.RoundToInt(distance) + " м", _distanceStyle);
                }
            }

            // Сундуки: пересобираем список не каждый кадр, а когда он устарел.
            if (_chests.Count == 0 || _chests[0] == null)
            {
                _chests.Clear();

                foreach (var chest in FindObjectsByType<Chest>(FindObjectsSortMode.None))
                    _chests.Add(chest);
            }

            foreach (var chest in _chests)
            {
                if (chest == null)
                    continue;

                DrawMarker(bar, up, forward, chest.transform.position - _player.position, ChestColor, 7f);
            }
        }

        private void DrawMarker(Rect bar, Vector3 up, Vector3 forward, Vector3 toTarget, Color color,
            float size)
        {
            if (!TryGetOffset(bar, up, forward, toTarget, out var x))
                return;

            HealthBarDrawer.DrawRect(new Rect(x - size * 0.5f, bar.y + 9f, size, size), color);
        }

        /// <summary>Горизонтальное смещение цели на шкале; false — цель за пределами обзора.</summary>
        private bool TryGetOffset(Rect bar, Vector3 up, Vector3 forward, Vector3 toTarget, out float x)
        {
            x = 0f;

            var flat = Vector3.ProjectOnPlane(toTarget, up);

            if (flat.sqrMagnitude < 0.0001f)
                return false;

            var angle = Vector3.SignedAngle(forward, flat.normalized, up);

            if (Mathf.Abs(angle) > VisibleAngle)
                return false;

            x = bar.x + BarWidth * 0.5f + angle / VisibleAngle * BarWidth * 0.5f;
            return true;
        }

        private void EnsureStyles()
        {
            _distanceStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.90f, 1f) }
            };
        }
    }
}
