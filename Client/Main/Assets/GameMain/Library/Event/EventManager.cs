using System;
using System.Collections.Generic;

namespace Dragon.Library
{
    /// <summary>
    /// 事件系统管理器
    /// </summary>
    public class EventManager
    {
        #region //静态变量

        //静态变量
        protected static EventManager _instance = null;

        //静态属性
        public static EventManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EventManager();
                    EventRootScript.Create(_instance);
                }
                return _instance;
            }
        }
        #endregion

        #region //成员变量
        //事件回调存储
        private Dictionary<int, List<EventSystemHandler>> _events = new Dictionary<int, List<EventSystemHandler>>(2048);
        //事件消息缓存
        private Queue<EventMessage> _eventMessageQueue = new Queue<EventMessage>(1024);
        //派发缓存，复用以减少每帧分配
        private List<EventMessage> _dispatchBuffer = new List<EventMessage>(64);

        //是否开启异步消息
        private bool _enableAsynMsg = false;
        #endregion

        #region//公共接口
        /// <summary>
        /// 注册事件函数调用。
        /// </summary>
        /// <returns>返回 True 表明回调添加成功，返回 False 表明回调添加失败。</returns>
        public EventSystemHandler Register(int eventType, EventSystemHandler eventHandler)
        {
            //一次只能注册一个回调函数，防止函数重复注册
            if (eventHandler == null)
            {
                LogError(string.Format("RegEventHandle:eventType={0},eventHandler=null", eventType));
                return null;
            }

            if (!_events.TryGetValue(eventType, out List<EventSystemHandler> list))
            {
                list = new List<EventSystemHandler>(4);
                _events[eventType] = list;
            }
            else
            {
                if (IndexOf(list, eventHandler) >= 0)
                {
                    return eventHandler;
                }
            }

            list.Add(eventHandler);
            return eventHandler;
        }

        /// <summary>
        /// 取消某个事件下的函数调用注册。
        /// </summary>
        public void UnRegister(int eventType, EventSystemHandler eventHandler)
        {
            if (eventHandler == null)
                return;

            if (_events.TryGetValue(eventType, out List<EventSystemHandler> list))
            {
                int idx = IndexOf(list, eventHandler);
                if (idx >= 0)
                {
                    list.RemoveAt(idx);
                }
            }
        }

        /// <summary>
        /// 删除某个事件类型。
        /// </summary>
        public void ClearEvent(int eventType)
        {
            if (_events.TryGetValue(eventType, out List<EventSystemHandler> list))
            {
                list.Clear();
                _events.Remove(eventType);
            }
        }

        /// <summary>
        /// 清除所有的事件。
        /// </summary>
        public void ClearAllEvent()
        {
            _events.Clear();
            _eventMessageQueue.Clear();
        }

        /// <summary>
        /// 事件管理器的心跳,用于处理异步消息
        /// </summary>
        public void Update()
        {
            if (_enableAsynMsg)
            {
                _dispatchBuffer.Clear();
                lock (_eventMessageQueue)
                {
                    var cnt = _eventMessageQueue.Count;
                    if (cnt > 0)
                    {
                        if (_dispatchBuffer.Capacity < cnt)
                        {
                            _dispatchBuffer.Capacity = cnt;
                        }

                        for (int i = 0; i < cnt; i++)
                        {
                            if (_eventMessageQueue.Count <= 0)
                            {
                                break;
                            }
                            var msg = _eventMessageQueue.Dequeue();
                            if (msg.IsDelayExec())
                            {
                                _eventMessageQueue.Enqueue(msg);
                            }
                            else
                            {
                                _dispatchBuffer.Add(msg);
                            }
                        }
                    }
                }

                for (int i = 0; i < _dispatchBuffer.Count; i++)
                {
                    var msg = _dispatchBuffer[i];
                    TriggerEvent(msg.EventType, msg.Param, msg.Sender, msg.Target);
                }
            }
        }

        /// <summary>
        /// 发送事件。
        /// </summary>
        public void TriggerEvent(int eventType, object param = null, object sender = null, object target = null, bool isSync = true, float delayTime = 0f)
        {
            //判断是不是同步执行
            if (!isSync)
            {   //如果是异步,那么就把事件消息缓存起来,并返回
                CacheEventMessage(sender, target, eventType, param, delayTime);
                return;
            }

            if (_events.TryGetValue(eventType, out List<EventSystemHandler> list))
            {
                if (list != null && list.Count > 0)
                {
                    var snapshot = list.ToArray();
                    if (target == null)
                    {//执行方法链里的所有方法
                        for (int i = 0; i < snapshot.Length; i++)
                        {
                            try
                            {
                                snapshot[i](param, sender);
                            }
                            catch (Exception ex)
                            {
                                LogError(ex.Message, ex.StackTrace);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < snapshot.Length; i++)
                        {
                            if (snapshot[i].Target == target)
                            {
                                try
                                {
                                    snapshot[i](param, sender);
                                }
                                catch (Exception ex)
                                {
                                    LogError(ex.Message, ex.StackTrace);
                                }

                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查事件定义是否有效
        /// </summary>
        /// <param name="type"></param>
        public void CheckEventDefineIsValid(Type type)
        {
            //获取所有常量定义
            var fields = type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            //只校验int类型的静态字段，避免非事件字段导致类型转换异常
            List<int> eventValues = new List<int>(fields.Length);
            //针对每个事件类都有一个基础值,定义为EID_BASE的值
            int baseId = -1;
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType != typeof(int))
                {
                    continue;
                }

                int eventValue = (int)fields[i].GetValue(null);
                eventValues.Add(eventValue);
                if (baseId == -1 && fields[i].Name == "EID_BASE")
                {
                    baseId = eventValue;
                }
            }
            //输出当前类的定义的数量
            Log("当前类[" + type.Name + "]定义的事件数:" + eventValues.Count);
            //判断是否重复
            bool bDuplication = false;
            for (int i = 0; i < eventValues.Count; i++)
            {
                for (int j = 0; j < eventValues.Count; j++)
                {
                    if (i != j && eventValues[i] == eventValues[j])
                    {
                        if (baseId > 0)
                        {
                            LogError("当前事件定义重复: EID_BASE + " + (eventValues[i] - baseId));
                        }
                        else
                        {
                            LogError("当前事件定义重复: " + eventValues[i]);
                        }
                        bDuplication = true;
                    }
                }
            }
            //如果有有重复,那么就触发异常
            if (bDuplication)
            {
                throw new Exception("当前类[" + type.Name + "]事件定义异常!!");
            }

        }


        /// <summary>
        /// 是否存在事件处理器
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        public bool IsExistEventHandler(int eventType)
        {
            if (_events.TryGetValue(eventType, out List<EventSystemHandler> list))
            {
                return list != null && list.Count > 0;
            }
            return false;
        }
        #endregion

        #region//私有函数

        //缓存事件消息
        private void CacheEventMessage(object sender, object target, int eventType, object param, float delayTime)
        {
            //开启异步消息
            _enableAsynMsg = true;

            //把消息添加到cache中
            lock (_eventMessageQueue)
            {
                _eventMessageQueue.Enqueue(new EventMessage(sender, target, eventType, param, delayTime));
            }
        }

        //从列表中获取Handler
        private int IndexOf(List<EventSystemHandler> list, EventSystemHandler handler)
        {
            var cnt = list.Count;
            for (int i = 0; i < cnt; i++)
            {
                if (list[i].Equals(handler))
                {
                    return i;
                }
            }
            return -1;
        }

        //日志
        private void Log(string msg)
        {
            UnityEngine.Debug.Log(msg);
        }
        //错误日志
        private void LogError(string msg, string detailmsg = "")
        {
            UnityEngine.Debug.LogError(msg + detailmsg);
        }
        #endregion 
    }

    //事件系统的处理句柄委托
    public delegate void EventSystemHandler(object param, object sender);
}
