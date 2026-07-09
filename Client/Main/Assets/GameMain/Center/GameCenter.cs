using System;
using System.Collections;
using UnityEngine;
using Dragon.Logic;
using Dragon.Resource;
using Dragon.Config;
using Dragon.GameInput;
using Dragon.GameData;
using Dragon.GameUI;
using Dragon.Audio;
using Dragon.Localization;
using Cysharp.Threading.Tasks;

namespace Dragon.Library
{
    public class GameCenter : MonoBehaviour
    {
        #region//私有变量
        //是否已经初始化完毕
        private static bool _isInitialized = false;
        public static bool IsInitialized { get { return _isInitialized; } }
        //是否挂起
        public static bool IsFocused { get; set; }
        #endregion

        #region//游戏中内核逻辑管理器
        public static ResourceManager ResourceManager = null;
        public static ResourcePreloadService ResourcePreloadService = null;
        public static ConfigManager ConfigManager = null;
        public static GameStateManager StateManager = null;
        public static EventManager EventManager = null;
        public static InputManager InputManager = null;
        public static GameDataManager DataManager = null;
        public static GameSaveManager SaveManager = null;
        public static GameUIManager GameUIManager = null;
        public static PopupManager PopupManager = null;
        public static ToastManager ToastManager = null;
        public static DebugLogManager DebugLogManager = null;
        public static UIQueueManager UIQueueManager = null;
        public static PoolManager PoolManager = null;
        public static SchedulerManager SchedulerManager = null;
        #endregion

        #region //游戏逻辑管理器
        public static LocalManager LocalManager = null;
        public static TerminalManager TerminalManager = null;
        public static AudioManager AudioManager = null;
        public static ItemManager ItemManager = null;
        public static MapSceneManager MapSceneManager = null;
        // public static RoundManager RoundManager = null;
        #endregion

        #region //初始化以及卸载处理
        //静态构造函数        
        public static void CreateSystem()
        {
            Application.targetFrameRate = 60;

            //内核和逻辑系统的创建
            NewCoreSystem();
            NewLogicSystem();

            Debug.Log("GameCenter Create Finished!!");
        }

        //游戏中心进行初始化处理
        public static void Initialize()
        {
            _isInitialized = false;
            CoroutinePool.AddTask(CoreInitialize(
            () =>
            {
                LogicInitialize();
                _isInitialized = true;
                ResetGameSpeedRate(); //初始化游戏速度
                StateManager.ChangeState(GameStateCode.Main);
            }));
        }
        //卸载
        public static void Uninitialize()
        {
            ResetGameSpeedRate();
            LogicUninitialize();
            CoreUninitialize();
            Debug.Log("GameCenter Uninitialize Finished!!");
        }

        //清理处理
        public static void ResetLogicSystem()
        {
            LogicUninitialize();
            NewLogicSystem();
            LogicInitialize();
        }
        #endregion

        #region 游戏速度
        public static void SetPause(bool isPause)
        {
            // MapManager.IsPause = isPause;            
        }

        public static bool IsPaused()
        {
            return false;
        }

        public static void SetGameSpeedRate(float speed)
        {
            TimeUtils.SetGameSpeedRate(speed);
        }

        public static void ResetGameSpeedRate()
        {
            TimeUtils.SetGameSpeedRate(1f);
        }
        #endregion

        #region//心跳处理        
        public static void Update(float deltaTime)
        {
            if (!_isInitialized) return;
            StateManager.Update(deltaTime);
            SchedulerManager.Update(deltaTime);
            TerminalManager.Update(deltaTime);
            MapSceneManager.Update(deltaTime);
        }

        public static void LateUpdate(float deltaTime)
        {
            if (!_isInitialized) return;
            MapSceneManager.LateUpdate(deltaTime);
        }

        public static void FixedUpdate(float deltaTime)
        {
            if (!_isInitialized) return;
        }
        #endregion

        #region//应用程序退出操作
        public static void ApplicationQuit()
        {
            Uninitialize();
            _isInitialized = false;
        }
        #endregion

        #region//私有函数处理

        #region//Core系统的方法
        //工具逻辑系统
        private static void NewCoreSystem()
        {
            StateManager = new GameStateManager();
            EventManager = EventManager.Instance;
            InputManager = InputManager.Instance;
            ResourceManager = ResourceManager.Instance;
            ResourcePreloadService = ResourcePreloadService.Instance;
            ConfigManager = ConfigManager.Instance;
            DataManager = GameDataManager.Instance;
            SaveManager = GameSaveManager.Instance;
            GameUIManager = GameUIManager.Instance;
            PopupManager = PopupManager.Instance;
            ToastManager = ToastManager.Instance;
            UIQueueManager = UIQueueManager.Instance;
            DebugLogManager = DebugLogManager.Instance;
            DebugLogManager.Init();
            PoolManager = PoolManager.Instance;
            SchedulerManager = SchedulerManager.Instance;

            Debug.Log("NewCoreSystem Finished!!");
        }
        //游戏中心进行初始化处理
        private static IEnumerator CoreInitialize(Action onCoreInitFinished)
        {
            Debug.Log("GameCenter Start CoreInitialize!");
            //初始化游戏状态管理器
            yield return null;
            StateManager.Initialize();
            ResourceManager.Initialize();
            InputManager.Init();

            // 异步初始化配置管理器
            Debug.Log("开始预加载配置资源...");
            ConfigManager.OnLoadProgress = null;
            yield return ConfigManager.Init().ToCoroutine();
            Debug.Log("游戏配置加载完成");

            Debug.Log("开始预加载基础资源...");
            yield return ResourcePreloadService.PreloadBootstrapResourcesAsync(null).ToCoroutine();
            Debug.Log("基础资源预加载完成");

            Debug.Log("开始加载用户数据");
            DataManager.Init();
            SaveManager.Init();
            SchedulerManager.Init();
            Debug.Log("用户数据加载完成");

            yield return null;
            onCoreInitFinished();
            Debug.Log("GameCenter End CoreInitialize!");
        }
        //卸载
        private static void CoreUninitialize()
        {
            StateManager.Uninitialize();
            EventManager.ClearAllEvent();
            InputManager.Shutdown();
            SchedulerManager.Shutdown();
            PoolManager.ClearAll();
            DebugLogManager.Shutdown();
        }
        #endregion

        #region //游戏逻辑的处理
        //游戏逻辑系统
        private static void NewLogicSystem()
        {
            LocalManager = LocalManager.Instance;
            TerminalManager = TerminalManager.Instance;
            AudioManager = AudioManager.Instance;
            ItemManager = ItemManager.Instance;
            MapSceneManager = MapSceneManager.Instance;
            // RoundManager = RoundManager.Instance;
            Debug.Log("NeLogicSystem Finished!!");
        }
        //游戏中心进行初始化处理
        private static void LogicInitialize()
        {
            Debug.Log("GameCenter Start LogicInitialize!");
            LocalManager.Init();
            TerminalManager.Init();
            AudioManager.Init();
            ItemManager.Init();
            MapSceneManager.Init();
            // RoundManager.Init();
            Debug.Log("GameCenter End LogicInitialize!");
        }


        //卸载
        private static void LogicUninitialize()
        {
            AudioManager.Shutdown();
            ItemManager.Clear();
            MapSceneManager.Clear();
            // RoundManager.Clear();
        }
        #endregion

        #endregion
    }
}