using System;
using UnityEngine;

namespace Dragon.Localization
{
    [Serializable]
    public class LocalString
    {
        [SerializeField]
        private string m_Key;
        
        private string m_CachedLocalizedValue;
        private LanguageType m_CachedLanguage;

        public string Key
        {
            get { return m_Key; }
            set 
            { 
                if (m_Key != value)
                {
                    m_Key = value;
                    InvalidateCache();
                }
            }
        }

        public string Value
        {
            get 
            {
                // 如果缓存无效，更新本地化值
                if (IsCacheInvalid())
                {
                    UpdateCachedValue();
                }
                
                return m_CachedLocalizedValue ?? m_Key;
            }
        }

        public LocalString() 
        {
            m_Key = string.Empty;
            m_CachedLanguage = LocalManager.Instance.CurrentLanguage;
        }

        public LocalString(string key)
        {
            m_Key = key ?? string.Empty;
            m_CachedLanguage = LocalManager.Instance.CurrentLanguage;
            UpdateCachedValue();
        }

        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// 当语言切换时，使缓存失效
        /// </summary>
        private bool IsCacheInvalid()
        {
            return m_CachedLocalizedValue == null || 
                   m_CachedLanguage != LocalManager.Instance.CurrentLanguage;
        }

        /// <summary>
        /// 更新缓存的本地化值
        /// </summary>
        private void UpdateCachedValue()
        {
            if (string.IsNullOrEmpty(m_Key))
            {
                m_CachedLocalizedValue = m_Key;
            }
            else
            {
                m_CachedLocalizedValue = LocalManager.Instance.GetLocalString(m_Key);
            }
            
            m_CachedLanguage = LocalManager.Instance.CurrentLanguage;
        }

        /// <summary>
        /// 使缓存失效，强制下次访问时重新获取本地化值
        /// </summary>
        private void InvalidateCache()
        {
            m_CachedLocalizedValue = null;
        }

        public static implicit operator string(LocalString localString)
        {
            return localString?.Value;
        }

        public static implicit operator LocalString(string key)
        {
            return new LocalString(key);
        }
    }
}