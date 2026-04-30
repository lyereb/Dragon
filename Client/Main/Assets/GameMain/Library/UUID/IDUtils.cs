using System;
using System.Collections.Generic;

using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dragon.Library
{
    public class IDUtils
    {
        // 自增计数器（用于同一毫秒内的去重）
        private static long _counter;
        // 上一次生成的时间戳
        private static long _lastTimestamp;
        // 基础的时间戳
        private static long _baseTimestamp = new DateTime(2025, 1, 1).Ticks / 10000;
        // 线程安全锁
        private static readonly object _lock = new object();

        //标记,保证首位设置位0,这样保证输出的值是正值 0000~0111
        private static long[] _flags = new long[] { 0, 1, 2, 3, 4, 5, 6, 7 };



        /// <summary>
        /// 生成全局唯一的随机long值（低重复概率）
        /// 基于时间戳+随机数+自增序列组合
        /// </summary>
        /// <returns>全局唯一的long值</returns>
        public static long NewId(int idx = 0)
        {
            lock (_lock)
            {
                // 获取当前毫秒级时间戳（作为基础值）
                long timestamp = DateTime.UtcNow.Ticks / 10000; // 转换为毫秒
                if (timestamp < _lastTimestamp)
                {
                    // 时钟回拨处理策略：抛出异常或等待
                    throw new Exception("Clock moved backwards, refusing to generate id");
                }
                // 如果同一毫秒内多次调用，自增计数器
                if (timestamp == _lastTimestamp)
                {
                    _counter++;
                    // 防止计数器溢出（超过1023则等待下一毫秒）
                    if (_counter > 1023)
                    {
                        while (timestamp <= _lastTimestamp)
                        {
                            timestamp = DateTime.UtcNow.Ticks / 10000;
                        }
                        _counter = 0;
                        _lastTimestamp = timestamp;
                    }
                }
                else
                {
                    // 不同毫秒重置计数器
                    _counter = 0;
                    _lastTimestamp = timestamp;
                }

                //生成随机数（10位）：使用GetInt32直接生成所需范围的随机数，避免分配字节数组       
                long random = RandomNumberGenerator.GetInt32(0, 1024); // 生成 [0, 1023] 的随机数

                //收集标记
                long flag = _flags[idx % _flags.Length];

                // 计算相对时间戳（40位），使用基准时间减少数值大小  (约 34.8 年)              
                long relativeTime = timestamp - _baseTimestamp;

                // 组合：标记(4位) + 时间戳（40位） + 计数器（10位） + 随机数（10位） → 64位
                long uniqueValue = ((flag & 0xF) << 60) | ((relativeTime & 0xFFFFFFFFFFL) << 20) | ((_counter & 0x3FF) << 10) | ((random & 0x3FF));

                return uniqueValue;
            }
        }

        /// <summary>
        /// 生成加密级别的全局唯一long值
        /// 适合安全性要求高的场景
        /// </summary>
        /// <returns>加密级唯一long值</returns>
        public static long GenerateCryptoUniqueLong()
        {
            byte[] bytes = new byte[8];
            RandomNumberGenerator.Fill(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        /// <summary>
        /// 检查两个生成的long值是否冲突（用于测试）
        /// </summary>
        public static bool CheckCollision(long value1, long value2)
        {
            return value1 == value2;
        }
    }
}
