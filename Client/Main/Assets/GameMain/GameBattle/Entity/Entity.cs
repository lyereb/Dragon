using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Dragon.Library;
using Cysharp.Threading.Tasks;
using Dragon.Resource;

public partial class Entity : FSMHostBase<EntityStateCode>
{
    public Entity() : base("EntityFSM")
    {
    }

    public EntityData Data { get; protected set; }

    public GameObject Skin { get; private set; }

    public TData GetData<TData>() where TData : EntityData
    {
        return Data as TData;
    }

    //移动控制器
    private IInputProvider _inputProvider;
    private EntityMovementController _movementController;
    private EntityAttackController _attackController;

    #region//公共接口--初始化
    //初始化Entity
    public async UniTask<bool> Initialize(EntityInitContext context)
    {
        try
        {
            //1. 初始化数据
            Data = CreateEntityData(context);

            //2. 初始化皮肤
            GameObject skinPrefab = await OnLoadSkin(context);
            if (skinPrefab == null)
            {
                Debug.LogError("Initialize Fail,OnLoadSkin return null!");
                return false;
            }
            Skin = UnityEngine.Object.Instantiate(skinPrefab);
            if (Skin == null)
            {
                Debug.LogError("Initialize Fail,instantiate skin prefab failed!");
                return false;
            }

            SetupController();

            if (!OnInitAfter())
            {
                Debug.LogError("Initialize Fail,OnInitAfter return false!");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Initialize Entity Fail,Except:" + ex.Message);
            return false;
        }
        return true;
    }
    //卸载
    public void Uninitialize()
    {
        OnUnInitBefore();
        DestroyFSM();
    }
    #endregion

    public void Update(float dt)
    {
        //状态机
        OnFSMUpdate(dt);
        OnUpdate(dt);
    }

    public void LateUpdate(float dt)
    {
        OnLateUpdate(dt);
    }

    #region 子类继承
    protected virtual EntityType GetEntityType()
    {
        return default;
    }

    protected virtual EntityData CreateEntityData(EntityInitContext context)
    {
        EntityData data = new EntityData();
        ApplyEntityData(data, context);
        return data;
    }

    protected virtual void ApplyEntityData(EntityData data, EntityInitContext context)
    {
        if (data == null || context == null)
        {
            return;
        }

        data.Position?.SetLogicPosition(context.SpawnLogicPosition);
    }

    protected virtual UniTask<GameObject> OnLoadSkin(EntityInitContext context)
    {
        string prefabName = GetSkinPrefabName(context);
        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogError($"{GetType().Name}.OnLoadSkin fail, prefabName is null or empty.");
            return UniTask.FromResult<GameObject>(null);
        }

        var loadTcs = new UniTaskCompletionSource<GameObject>();
        _ = ResourceManager.Instance.LoadAsync<GameObject>(
            prefabName,
            prefab => loadTcs.TrySetResult(prefab),
            exception => loadTcs.TrySetException(exception));
        return loadTcs.Task;
    }

    protected virtual string GetSkinPrefabName(EntityInitContext context)
    {
        return string.Empty;
    }

    protected override bool OnFSMSetup()
    {
        FSM.Register(new EntityIdleState());
        return true;
    }

    protected virtual bool OnInitAfter()
    {
        return true;
    }

    protected virtual void OnUpdate(float dt)
    {
        _movementController?.Update(this, _inputProvider, dt);
        _attackController?.Update(this, _inputProvider, dt);
    }

    protected virtual void OnLateUpdate(float dt)
    {
    }

    protected virtual bool OnUnInitBefore()
    {
        _movementController?.ClearState();
        _attackController?.ClearState();
        _inputProvider = null;
        _movementController = null;
        _attackController = null;
        return true;
    }

    protected virtual void SetupController()
    {
        _inputProvider = CreateInputProvider();
        _movementController = new EntityMovementController();
        _attackController = new EntityAttackController();
    }

    protected virtual IInputProvider CreateInputProvider()
    {
        return null;
    }
    #endregion
}
