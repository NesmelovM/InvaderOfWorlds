using System.Collections.Generic;
using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Процедурный звук: клипы генерируются кодом, файлов в проекте нет.
    ///
    /// Для прототипа это честнее, чем тишина: удар, смерть, выстрел и портал
    /// сразу дают обратную связь, а позже их можно заменить на нормальные записи,
    /// не меняя точки вызова.
    /// </summary>
    public static class SoundBank
    {
        private const int SampleRate = 44100;

        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        private static float _lastHitTime;

        public static void Hit(Vector3 position)
        {
            // В густой драке десятки ударов не должны превращаться в кашу.
            if (Time.time - _lastHitTime < 0.04f)
                return;

            _lastHitTime = Time.time;
            Play("hit", position, 0.35f);
        }

        public static void Death(Vector3 position)
        {
            Play("death", position, 0.45f);
        }

        public static void Hurt(Vector3 position)
        {
            Play("hurt", position, 0.5f);
        }

        public static void Shot(Vector3 position)
        {
            Play("shot", position, 0.18f);
        }

        public static void Portal(Vector3 position)
        {
            Play("portal", position, 0.6f);
        }

        private static void Play(string key, Vector3 position, float volume)
        {
            var clip = GetClip(key);

            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        private static AudioClip GetClip(string key)
        {
            if (Clips.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var clip = key switch
            {
                "hit" => CreateNoise("hit", 0.09f, 0.6f),
                "death" => CreateTone("death", 0.3f, 320f, 70f, 0.5f),
                "hurt" => CreateTone("hurt", 0.18f, 180f, 90f, 0.6f),
                "shot" => CreateTone("shot", 0.07f, 900f, 500f, 0.4f),
                "portal" => CreateTone("portal", 0.9f, 220f, 880f, 0.4f),
                _ => null
            };

            if (clip != null)
                Clips[key] = clip;

            return clip;
        }

        /// <summary>Короткий шум с затуханием — звук удара.</summary>
        private static AudioClip CreateNoise(string name, float duration, float volume)
        {
            var samples = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[samples];
            var random = new System.Random(12345);

            for (var i = 0; i < samples; i++)
            {
                var progress = i / (float)samples;
                var decay = Mathf.Pow(1f - progress, 3f);
                data[i] = ((float)random.NextDouble() * 2f - 1f) * decay * volume;
            }

            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Тон с плавным сдвигом частоты — смерть, выстрел, портал.</summary>
        private static AudioClip CreateTone(string name, float duration, float fromHz, float toHz, float volume)
        {
            var samples = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[samples];
            var phase = 0f;

            for (var i = 0; i < samples; i++)
            {
                var progress = i / (float)samples;
                var frequency = Mathf.Lerp(fromHz, toHz, progress);
                phase += 2f * Mathf.PI * frequency / SampleRate;

                var decay = Mathf.Pow(1f - progress, 1.6f);
                data[i] = Mathf.Sin(phase) * decay * volume;
            }

            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
