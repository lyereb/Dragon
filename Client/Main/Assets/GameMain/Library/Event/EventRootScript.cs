using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 挂接事件系统的脚本
    /// </summary>
    public class EventRootScript : MonoBehaviour
    {
        private static EventRootScript _instance;
        private static EventManager _manager;
        internal static void Create(EventManager manager)
        {
            _manager = manager;
            if (_instance == null)
            {
                var go = new GameObject("[EventSystem]");
                _instance = go.AddComponent<EventRootScript>();
                DontDestroyOnLoad(go);
            }
        }


        private void Update()
        {
            if (_manager != null)
            {
                _manager.Update();
            }
        }

        private void OnDestroy()
        {
            _manager = null;
            _instance = null;
        }
    }
}
