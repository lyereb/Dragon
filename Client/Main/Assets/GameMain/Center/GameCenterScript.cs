using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dragon.Library
{
    public class GameCenterScript : MonoBehaviour
    {

        private static GameCenterScript _instance;

        public static void Create()
        {
            //创建GameMain
            if (_instance == null)
            {
                GameObject go = new GameObject("[GameMain]");
                _instance = go.AddComponent<GameCenterScript>();
            }
        }

        private bool _isCreated = false;

        /// <summary>进入后台时的 realtimeSinceStartup；&lt; 0 表示当前不在后台计时中。</summary>
        private float _backgroundEnterRealtime = -1f;

        private void Awake()
        {
            Debug.Log("GameCenterScript Awake");
            Application.quitting -= DoApplicationQuit;
            Application.quitting += DoApplicationQuit;
            TimeUtils.Update(Time.deltaTime);
        }

        private void Start()
        {
            Debug.Log("GameCenterScript Start");
            DontDestroyOnLoad(gameObject);

            if (!_isCreated)
            {
                _isCreated = true;
                GameCenter.CreateSystem();
                GameCenter.Initialize();
                SceneManager.LoadScene(GlobalDefine.MainMenuSceneName);
            }
            Debug.Log("GameCenterScript End");
        }

        void Update()
        {
            if (_isCreated)
            {
                try
                {
                    TimeUtils.Update(Time.deltaTime);
                    GameCenter.Update(Time.deltaTime);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        void FixedUpdate()
        {
            if (_isCreated)
            {
                GameCenter.FixedUpdate(Time.deltaTime);
            }
        }

        void LateUpdate()
        {
            if (_isCreated)
            {
                GameCenter.LateUpdate(Time.deltaTime);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                MarkBackgroundEnter("OnApplicationPause");
            }
            else
            {
                LogBackgroundResume("OnApplicationPause");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                MarkBackgroundEnter("OnApplicationFocus");
            }
            else
            {
                LogBackgroundResume("OnApplicationFocus");
            }
        }

        private void MarkBackgroundEnter(string source)
        {
            // Pause / Focus 可能先后触发，只记第一次进入后台的时间。
            if (_backgroundEnterRealtime < 0f)
            {
                _backgroundEnterRealtime = Time.realtimeSinceStartup;
            }

            Debug.Log(
                $"[GameCenterScript] {source} enter background, " +
                $"focus={Application.isFocused}, realtime={Time.realtimeSinceStartup:F3}");
        }

        private void LogBackgroundResume(string source)
        {
            // Pause / Focus 回前台可能连着触发两次，时长只在第一次有效计时上打印。
            if (_backgroundEnterRealtime < 0f)
            {
                Debug.Log(
                    $"[GameCenterScript] {source} resume foreground (already resumed), " +
                    $"realtime={Time.realtimeSinceStartup:F3}");
                return;
            }

            float duration = Time.realtimeSinceStartup - _backgroundEnterRealtime;
            _backgroundEnterRealtime = -1f;

            Debug.Log(
                $"[GameCenterScript] {source} resume foreground, " +
                $"backgroundDuration={duration:F3}s, realtime={Time.realtimeSinceStartup:F3}");
        }

        void DoApplicationQuit()
        {
            if (_isCreated)
            {
                GameCenter.ApplicationQuit();
            }
        }
    }
}