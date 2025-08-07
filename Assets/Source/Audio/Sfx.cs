using UnityEngine;

namespace BrawlLine.Audio
{
    [CreateAssetMenu(fileName = "NewSfx", menuName = "BrawlLine/Audio/Sfx")]
    public class Sfx : ScriptableObject
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(0.1f, 3f)] private float pitch = 1f;
        [SerializeField] private bool loop;

        public AudioClip Clip => clip;
        public float Volume => volume;
        public float Pitch => pitch;
        public bool Loop => loop;
    }
}