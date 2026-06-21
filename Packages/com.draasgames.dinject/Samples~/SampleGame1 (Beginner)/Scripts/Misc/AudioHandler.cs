using System;
using UnityEngine;

namespace DInject.Asteroids
{
    public partial class AudioHandler : IInitializable, IDisposable
    {
        readonly GameEvents _events;
        readonly Settings _settings;
        readonly AudioSource _audioSource;

        public AudioHandler(
            AudioSource audioSource,
            Settings settings,
            GameEvents events)
        {
            _events = events;
            _settings = settings;
            _audioSource = audioSource;
        }

        public void Initialize()
        {
            _events.ShipCrashed += OnShipCrashed;
        }

        public void Dispose()
        {
            _events.ShipCrashed -= OnShipCrashed;
        }

        void OnShipCrashed()
        {
            _audioSource.PlayOneShot(_settings.CrashSound);
        }

        [Serializable]
        public partial class Settings
        {
            public AudioClip CrashSound;
        }
    }
}
