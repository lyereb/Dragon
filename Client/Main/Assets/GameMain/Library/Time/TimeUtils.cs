using System;
using System.Text;
using UnityEngine;
using Dragon.Library;

namespace Dragon.Library
{
    /// <summary>
    /// 时间的帮助类
    /// </summary>
    public class TimeUtils
    {
        private static DateTime _notZoneStartTime = new DateTime(1970, 1, 1);
        // private static DateTime _localZoneStartTime = TimeZone.CurrentTimeZone.ToLocalTime(_notZoneStartTime);

        private static int _deltaTimeMs = 0;
        private static float _deltaTime = 0f;
        private static float _gameSpeedRate = 1f;
        

        //每帧的时间间隔,秒
        public static float DeltaTime { get { return _deltaTime; } }
        //每帧的时间间隔,毫秒
        public static int DeltaTimeMs { get { return _deltaTimeMs; } }
        //游戏速度
        public static float GameSpeedRate { get { return _gameSpeedRate; } }

        public static void Update(float dt)
        {
            _deltaTime = dt * _gameSpeedRate;
            _deltaTimeMs = UnityEngine.Mathf.RoundToInt(_deltaTime * 1000);
        }

        //改变游戏速度
        public static void SetGameSpeedRate(float speedRate)
        {
            _gameSpeedRate = speedRate;
        }    
    }
}

