using System;
using Cysharp.Threading.Tasks;
using Dragon.Resource;
using UnityEngine;

/// <summary>
/// 按配置里的预制体名称加载战斗表现，并把逻辑坐标换成渲染坐标。
/// </summary>
public static class CombatView
{
    private const string RootName = "CombatViewRoot";

    public static void Load(string prefabName, Action<GameObject> onLoaded, Func<bool> isCancelled)
    {
        if (string.IsNullOrEmpty(prefabName) || onLoaded == null)
        {
            return;
        }

        LoadAsync(prefabName, onLoaded, isCancelled).Forget();
    }

    public static void Sync(GameObject view, Vector3 logicPosition)
    {
        if (view == null)
        {
            return;
        }

        view.transform.position = new Vector3(logicPosition.x, logicPosition.y, 0f);
    }

    public static void Destroy(ref GameObject view)
    {
        if (view == null)
        {
            return;
        }

        UnityEngine.Object.Destroy(view);
        view = null;
    }

    private static async UniTaskVoid LoadAsync(string prefabName, Action<GameObject> onLoaded, Func<bool> isCancelled)
    {
        GameObject prefab = await ResourceManager.Instance.LoadAssetAsync<GameObject>(prefabName);
        if (prefab == null || (isCancelled != null && isCancelled()))
        {
            return;
        }

        GameObject view = UnityEngine.Object.Instantiate(prefab);
        if (isCancelled != null && isCancelled())
        {
            UnityEngine.Object.Destroy(view);
            return;
        }

        view.transform.SetParent(GetRoot(), false);
        onLoaded(view);
    }

    private static Transform GetRoot()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
        }

        return root.transform;
    }
}
