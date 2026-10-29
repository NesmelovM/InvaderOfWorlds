using UnityEngine;

namespace InvaderOfWorlds
{
    /// <summary>
    /// Фон планеты: тихий ветер, который звучит всё время забега.
    ///
    /// Отдельный источник с зацикленным клипом — так биомы не молчат,
    /// а громкость не мешает ударам и шагам.
    /// </summary>
    [DisallowMultipleComponent]
    public class Ambience : MonoBehaviour
    {
        private AudioSource _source;

        private void Start()
        {
            var clip = SoundBank.Wind();

            if (clip == null)
                return;

            _source = gameObject.AddComponent<AudioSource>();
            _source.clip = clip;
            _source.loop = true;
            _source.volume = 0.12f;
            _source.pitch = Random.Range(0.85f, 1.05f);
            _source.spatialBlend = 0f;
            _source.playOnAwake = false;
            _source.Play();
        }

        /// <summary>Громкость ветра: в открытой пустыне громче, под землёй тише.</summary>
        public void SetVolume(float volume)
        {
            if (_source != null)
                _source.volume = Mathf.Clamp01(volume);
        }
    }
}
