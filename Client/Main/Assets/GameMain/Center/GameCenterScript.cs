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

        void DoApplicationQuit()
        {
            if (_isCreated)
            {
                GameCenter.ApplicationQuit();
            }
        }
    }
}