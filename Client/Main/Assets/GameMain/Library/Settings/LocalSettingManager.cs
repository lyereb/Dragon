using Dragon.Audio;
using Dragon.Localization;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 本地偏好设置：语言、音量、教程已读等；统一经 PlayerPrefs 读写并应用到对应 Manager。
    /// </summary>
    public class LocalSettingManager
    {
        private const string KeyLanguage = "local_setting_language";
        private const string KeyBgmVolume = "local_setting_bgm_volume";
        private const string KeyAmbientVolume = "local_setting_ambient_volume";
        private const string KeySfxVolume = "local_setting_sfx_volume";
        private const string KeySoundEnabled = "local_setting_sound_enabled";
        private const string TutorialShownKeyPrefix = "tutorial_shown_";

        #region Singleton

        protected static LocalSettingManager _instance;

        public static LocalSettingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LocalSettingManager();
                }

                return _instance;
            }
        }

        #endregion

        private bool _initialized;
        private LanguageType _language = LanguageType.SimplifiedChinese;
        private float _bgmVolume = GlobalDefine.DefaultBgmVolume;
        private float _ambientVolume = GlobalDefine.DefaultAmbientVolume;
        private float _sfxVolume = GlobalDefine.DefaultSfxVolume;
        private bool _soundEnabled = true;

        public LanguageType Language => _language;
        public float BgmVolume => _bgmVolume;
        public float AmbientVolume => _ambientVolume;
        public float SfxVolume => _sfxVolume;
        /// <summary>全局声音开关；关闭时三类音量实际输出为 0，偏好值仍保留。</summary>
        public bool IsSoundEnabled => _soundEnabled;

        /// <summary>
        /// 从 PlayerPrefs 加载并应用到 <see cref="LocalManager"/> / <see cref="AudioManager"/>。
        /// 须在二者 Init 之后调用。
        /// </summary>
        public void Init()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            LoadFromPrefs();
            ApplyLanguage(save: false);
            ApplyVolumes(save: false);
            Debug.Log(
                $"[LocalSettingManager] Init. language={_language}, soundEnabled={_soundEnabled}, " +
                $"bgm={_bgmVolume:F2}, ambient={_ambientVolume:F2}, sfx={_sfxVolume:F2}");
        }

        public void Clear()
        {
            _initialized = false;
        }

        #region Language

        public void SetLanguage(LanguageType language)
        {
            if (_language == language)
            {
                ApplyLanguage(save: false);
                return;
            }

            _language = language;
            ApplyLanguage(save: true);
            EventManager.Instance.TriggerEvent(MainEventDefine.EID_LOCAL_SETTING_LANGUAGE_CHANGED);
        }

        #endregion

        #region Volume

        /// <summary>切换全局声音开关。</summary>
        public void ToggleSoundEnabled()
        {
            SetSoundEnabled(!_soundEnabled);
        }

        /// <summary>设置全局声音开关；关闭时 BGM/Ambient/SFX 实际输出为 0。</summary>
        public void SetSoundEnabled(bool enabled)
        {
            if (_soundEnabled == enabled)
            {
                ApplyVolumes(save: false);
                return;
            }

            _soundEnabled = enabled;
            ApplyVolumes(save: true);
            EventManager.Instance.TriggerEvent(MainEventDefine.EID_LOCAL_SETTING_VOLUME_CHANGED);
        }

        public void SetBgmVolume(float volume)
        {
            float next = Mathf.Clamp01(volume);
            if (Mathf.Approximately(_bgmVolume, next))
            {
                ApplyVolumes(save: false);
                return;
            }

            _bgmVolume = next;
            ApplyVolumes(save: true);
            EventManager.Instance.TriggerEvent(MainEventDefine.EID_LOCAL_SETTING_VOLUME_CHANGED);
        }

        public void SetAmbientVolume(float volume)
        {
            float next = Mathf.Clamp01(volume);
            if (Mathf.Approximately(_ambientVolume, next))
            {
                ApplyVolumes(save: false);
                return;
            }

            _ambientVolume = next;
            ApplyVolumes(save: true);
            EventManager.Instance.TriggerEvent(MainEventDefine.EID_LOCAL_SETTING_VOLUME_CHANGED);
        }

        public void SetSfxVolume(float volume)
        {
            float next = Mathf.Clamp01(volume);
            if (Mathf.Approximately(_sfxVolume, next))
            {
                ApplyVolumes(save: false);
                return;
            }

            _sfxVolume = next;
            ApplyVolumes(save: true);
            EventManager.Instance.TriggerEvent(MainEventDefine.EID_LOCAL_SETTING_VOLUME_CHANGED);
        }

        /// <summary>
        /// 一次性设置三类音量（设置页滑条批量提交时用）。
        /// </summary>
        public void SetVolumes(float bgm, float ambient, float sfx)
        {
            _bgmVolume = Mathf.Clamp01(bgm);
            _ambientVolume = Mathf.Clamp01(ambient);
            _sfxVolume = Mathf.Clamp01(sfx);
            ApplyVolumes(save: true);
            EventManager.Instance.TriggerEvent(MainEventDefine.EID_LOCAL_SETTING_VOLUME_CHANGED);
        }

        #endregion

        #region Tutorial

        public bool HasTutorialShown(string popupName)
        {
            if (string.IsNullOrEmpty(popupName))
            {
                return false;
            }

            return GetInt(BuildTutorialKey(popupName), 0) == 1;
        }

        public void MarkTutorialShown(string popupName)
        {
            if (string.IsNullOrEmpty(popupName))
            {
                return;
            }

            SetInt(BuildTutorialKey(popupName), 1, save: true);
        }

        public void ClearTutorialShown(string popupName)
        {
            if (string.IsNullOrEmpty(popupName))
            {
                return;
            }

            DeleteKey(BuildTutorialKey(popupName), save: true);
        }

        private static string BuildTutorialKey(string popupName)
        {
            return TutorialShownKeyPrefix + popupName;
        }

        #endregion

        #region PlayerPrefs 封装

        public int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public void SetInt(string key, int value, bool save = true)
        {
            PlayerPrefs.SetInt(key, value);
            if (save)
            {
                PlayerPrefs.Save();
            }
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        public void SetFloat(string key, float value, bool save = true)
        {
            PlayerPrefs.SetFloat(key, value);
            if (save)
            {
                PlayerPrefs.Save();
            }
        }

        public string GetString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public void SetString(string key, string value, bool save = true)
        {
            PlayerPrefs.SetString(key, value ?? string.Empty);
            if (save)
            {
                PlayerPrefs.Save();
            }
        }

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public void DeleteKey(string key, bool save = true)
        {
            if (string.IsNullOrEmpty(key) || !PlayerPrefs.HasKey(key))
            {
                return;
            }

            PlayerPrefs.DeleteKey(key);
            if (save)
            {
                PlayerPrefs.Save();
            }
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }

        #endregion

        #region Private

        private void LoadFromPrefs()
        {
            int languageValue = GetInt(KeyLanguage, (int)LanguageType.SimplifiedChinese);
            if (!System.Enum.IsDefined(typeof(LanguageType), languageValue))
            {
                languageValue = (int)LanguageType.SimplifiedChinese;
            }

            _language = (LanguageType)languageValue;
            _bgmVolume = Mathf.Clamp01(GetFloat(KeyBgmVolume, GlobalDefine.DefaultBgmVolume));
            _ambientVolume = Mathf.Clamp01(GetFloat(KeyAmbientVolume, GlobalDefine.DefaultAmbientVolume));
            _sfxVolume = Mathf.Clamp01(GetFloat(KeySfxVolume, GlobalDefine.DefaultSfxVolume));
            _soundEnabled = GetInt(KeySoundEnabled, 1) != 0;
        }

        private void ApplyLanguage(bool save)
        {
            LocalManager.Instance.SwitchLanguage(_language);
            if (save)
            {
                SetInt(KeyLanguage, (int)_language, save: true);
            }
        }

        private void ApplyVolumes(bool save)
        {
            AudioManager audio = AudioManager.Instance;
            if (audio != null)
            {
                float scale = _soundEnabled ? 1f : 0f;
                audio.BgmVolume = _bgmVolume * scale;
                audio.AmbientVolume = _ambientVolume * scale;
                audio.SfxVolume = _sfxVolume * scale;
            }

            if (save)
            {
                SetFloat(KeyBgmVolume, _bgmVolume, save: false);
                SetFloat(KeyAmbientVolume, _ambientVolume, save: false);
                SetFloat(KeySfxVolume, _sfxVolume, save: false);
                SetInt(KeySoundEnabled, _soundEnabled ? 1 : 0, save: false);
                PlayerPrefs.Save();
            }
        }

        #endregion
    }
}
