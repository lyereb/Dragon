using UnityEngine;
using UnityEngine.SceneManagement;
using Dragon.Config;
using cfg.dragon;
using Cysharp.Threading.Tasks;

namespace Dragon.Logic
{
    public class MapSceneManager
    {
        #region //静态变量
        protected static MapSceneManager _instance = null;

        public static MapSceneManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MapSceneManager();
                }
                return _instance;
            }
        }
        #endregion

        public MapData MapData { get; private set; }

        private bool _isEntityLoaded = false;

        public bool IsEntityLoaded => _isEntityLoaded;

        public EntitySpawnner EntitySpawnner { get; private set; }
        public EntityManager EntityManager { get; private set; }
        public BattleCombat BattleCombat { get; private set; }
        public CameraManager CameraManager { get; private set; }
        public Lighting2DManager Lighting2DManager { get; private set; }

        public void InitEntitySpawnner()
        {
            GameObject spawnRoot = GameObject.Find("SpawnRoot");
            if (spawnRoot == null)
            {
                Debug.LogError("MapSceneManager.InitEntitySpawnner fail, can not find GameObject named 'SpawnRoot' in scene.");
                return;
            }

            EntitySpawnner = new EntitySpawnner();
            EntitySpawnner.Initialize(spawnRoot.transform);
            EntityManager = new EntityManager();
            EntityManager.Initialize();
            BattleCombat = new BattleCombat();
            BattleCombat.Initialize(EntityManager);
            CameraManager = new CameraManager();
            CameraManager.Initialize();
            // Lighting2DManager = new Lighting2DManager();
            // Lighting2DManager.Initialize();
        }

        public async UniTask SpawnAllEntities()
        {
            if (EntitySpawnner == null || EntityManager == null)
            {
                Debug.LogError("MapSceneManager.SpawnAllEntities fail, EntitySpawnner or EntityManager is null.");
                return;
            }

            EntityManager.AddEntities(await EntitySpawnner.SpawnAll());

            Player player = await EntitySpawnner.SpawnPlayer();
            EntityManager.AddEntity(player);
            CameraManager.BindFollowTarget(player);

            EntitySpawnner.OnFinishSpawn();
        }

        public void LoadSceneAndSpawnAsync(int mapId, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            var mapConfig = ConfigManager.Instance.Tables.Tbmap.Get(mapId);
            if (mapConfig == null)
            {
                Debug.LogError($"MapSceneManager.LoadSceneAndSpawnAsync fail, can not find tbmap config by mapId: {mapId}");
                return;
            }

            string sceneName = mapConfig.SceneName;
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"MapSceneManager.LoadSceneAndSpawnAsync fail, sceneName is null or empty. mapId: {mapId}");
                return;
            }

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            if (op == null)
            {
                Debug.LogError($"MapSceneManager.LoadSceneAndSpawnAsync fail, can not load scene: {sceneName}");
                return;
            }

            MapData = new MapData();
            MapData.Initialize(mapConfig);
            _isEntityLoaded = false;

            op.completed += _ =>
            {
                InitEntitySpawnner();
                SpawnAllEntitiesAfterSceneLoaded().Forget();
            };
        }

        private async UniTask SpawnAllEntitiesAfterSceneLoaded()
        {
            await SpawnAllEntities();
            _isEntityLoaded = true;
        }

        public void Init()
        {
            Debug.Log("MapSceneManager Init");
        }

        public void Clear()
        {
            _isEntityLoaded = false;
            CameraManager?.Clear();
            Lighting2DManager?.Clear();
            BattleCombat?.Clear();
            Debug.Log("MapSceneManager Clear");
        }

        public void Update(float deltaTime)
        {
            if (!_isEntityLoaded)
            {
                return;
            }

            EntityManager.Update(deltaTime);
            BattleCombat?.Update(deltaTime);
        }

        public void LateUpdate(float deltaTime)
        {
            if (!_isEntityLoaded)
            {
                return;
            }

            EntityManager.LateUpdate(deltaTime);
            CameraManager.LateUpdate(deltaTime);
        }

        public void QuitMap()
        {
            _isEntityLoaded = false;
            EntityManager.Clear();
            BattleCombat?.Clear();
            CameraManager.Clear();
            Lighting2DManager?.Clear();
            EntitySpawnner = null;
            EntityManager = null;
            BattleCombat = null;
            CameraManager = null;
            Lighting2DManager = null;
            MapData = null;

            SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        }
    }
}