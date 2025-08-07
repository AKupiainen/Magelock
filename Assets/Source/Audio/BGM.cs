using UnityEngine;

namespace BrawlLine.Audio
{
    [CreateAssetMenu(fileName = "NewBGM", menuName = "BrawlLine/Audio/BGM")]
    public class BGM : ScriptableObject
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private bool loop = true;

        public AudioClip Clip => clip;
        public float Volume => volume;
        public bool Loop => loop;
    }
}