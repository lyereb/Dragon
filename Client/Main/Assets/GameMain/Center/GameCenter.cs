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
        #endregion

        #region//游戏中内核逻辑管理器
        public static ResourceManager ResourceManager = null;
        public static ResourcePreloadService ResourcePreloadService = null;
        public static AddressablesRemoteService AddressablesRemoteService = null;
        public static ConfigManager ConfigManager = null;
        public static GameStateManager StateManager = null;
        public static EventManager EventManager = null;
        public static InputManager InputManager = null;
        public static GameDataManager DataManager = null;
        public static GameSaveManager SaveManager = null;
        public static GameUIManager GameUIManager = null;
        public static PopupManager PopupManager = null;
        public static ToastManager ToastManager = null;
        public static GMPanelManager GMPanelManager = null;
        public static UIQueueManager UIQueueManager = null;
        public static PoolManager PoolManager = null;
        #endregion

        #region //游戏逻辑管理器
        public static LocalManager LocalManager = null;
        public static TerminalManager TerminalManager = null;
        public static AudioManager AudioManager = null;
        public static LocalSettingManager LocalSettingManager = null;
        public static ItemManager ItemManager = null;
        public static MapSceneManager MapSceneManager = null;
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
            CoreInitializeAsync().Forget();
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
            AddressablesRemoteService = AddressablesRemoteService.Instance;
            ConfigManager = ConfigManager.Instance;
            DataManager = GameDataManager.Instance;
            SaveManager = GameSaveManager.Instance;
            GameUIManager = GameUIManager.Instance;
            PopupManager = PopupManager.Instance;
            ToastManager = ToastManager.Instance;
            UIQueueManager = UIQueueManager.Instance;
            GMPanelManager = GMPanelManager.Instance;
            GMPanelManager.Init();
            PoolManager = PoolManager.Instance;

            Debug.Log("NewCoreSystem Finished!!");
        }
        //游戏中心进行初始化处理
        private static async UniTask CoreInitializeAsync()
        {
            Debug.Log("GameCenter Start CoreInitialize!");
            //初始化游戏状态管理器
            await UniTask.Yield();
            StateManager.Initialize();
            ResourceManager.Initialize();
            InputManager.Init();

            // 下载 / 加载资源期间进入 Update，由状态机打开对应 UI
            StateManager.ChangeState(GameStateCode.Update);
            await UniTask.Yield();

            Debug.Log("开始准备 Addressables 资源（Catalog/下载）...");
            await AddressablesRemoteService.PrepareStartupResourcesAsync(
                ResourcePreloadService.GetStartupDownloadKeys(),
                null);
            Debug.Log("Addressables 资源准备完成");

            // 异步初始化配置管理器
            Debug.Log("开始预加载配置资源...");
            ConfigManager.OnLoadProgress = null;
            await ConfigManager.Init();
            Debug.Log("游戏配置加载完成");

            // 资源的预加载可以移动到其他地方， 比如某个初始化界面打开之后再进行预加载
            Debug.Log("开始预加载基础资源...");
            await ResourcePreloadService.PreloadBootstrapResourcesAsync(null);
            Debug.Log("基础资源预加载完成");

            Debug.Log("开始加载用户数据");
            DataManager.Init();
            SaveManager.Init();
            Debug.Log("用户数据加载完成");

            await UniTask.Yield();
            LogicInitialize();
            _isInitialized = true;
            ResetGameSpeedRate(); //初始化游戏速度

            //进入大厅
            StateManager.ChangeState(GameStateCode.Main);
            Debug.Log("GameCenter End CoreInitialize!");
        }
        //卸载
        private static void CoreUninitialize()
        {
            StateManager.Uninitialize();
            EventManager.ClearAllEvent();
            InputManager.Shutdown();
            PoolManager.ClearAll();
            ResourceManager.ClearAll();
            GMPanelManager.Shutdown();
        }
        #endregion

        #region //游戏逻辑的处理
        //游戏逻辑系统
        private static void NewLogicSystem()
        {
            LocalManager = LocalManager.Instance;
            TerminalManager = TerminalManager.Instance;
            AudioManager = AudioManager.Instance;
            LocalSettingManager = LocalSettingManager.Instance;
            ItemManager = ItemManager.Instance;
            MapSceneManager = MapSceneManager.Instance;
            Debug.Log("NeLogicSystem Finished!!");
        }
        //游戏中心进行初始化处理
        private static void LogicInitialize()
        {
            Debug.Log("GameCenter Start LogicInitialize!");
            LocalManager.Init();
            TerminalManager.Init();
            AudioManager.Init();
            LocalSettingManager.Init();
            ItemManager.Init();
            MapSceneManager.Init();
            Debug.Log("GameCenter End LogicInitialize!");
        }


        //卸载
        private static void LogicUninitialize()
        {
            AudioManager.Shutdown();
            ItemManager.Clear();
            LocalSettingManager.Clear();
            MapSceneManager.Clear();
        }
        #endregion

        #endregion
    }
}