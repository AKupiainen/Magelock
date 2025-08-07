using System;
using UnityEngine;

namespace BrawlLine.Player
{
    [Serializable]
    public class SettingsData
    {
        public SystemLanguage language;
        public float masterVolume;
        public float musicVolume;
        public float soundEffectsVolume;
        public bool vibrationEnabled;

        public SettingsData()
        {
            masterVolume = 1f;
            musicVolume = 0.5f;
            soundEffectsVolume = 0.5f;
            vibrationEnabled = true;
            language = SystemLanguage.English;
        }

        public SettingsData(SettingsData other)
        {
            masterVolume = other.masterVolume;
            musicVolume = other.musicVolume;
            soundEffectsVolume = other.soundEffectsVolume;
            language = other.language;
            vibrationEnabled = other.vibrationEnabled;
        }
    }
}