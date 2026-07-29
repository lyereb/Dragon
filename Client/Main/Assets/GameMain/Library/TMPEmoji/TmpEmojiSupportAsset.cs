using System.Collections.Generic;
using UnityEngine;

namespace Dragon.Library
{
    /// <summary>
    /// 打图集时生成的 Unicode emoji 白名单，运行时过滤未支持表情。
    /// </summary>
    [CreateAssetMenu(fileName = "TmpEmojiSupport", menuName = "TMP/Emoji Support Asset", order = 110)]
    public sealed class TmpEmojiSupportAsset : ScriptableObject
    {
        [Tooltip("支持的 emoji 码点（十进制）")]
        [SerializeField]
        private int[] _unicodes = System.Array.Empty<int>();

        private HashSet<int> _lookup;

        public int Count => _unicodes != null ? _unicodes.Length : 0;

        public void SetUnicodes(IList<uint> unicodes)
        {
            if (unicodes == null || unicodes.Count == 0)
            {
                _unicodes = System.Array.Empty<int>();
            }
            else
            {
                _unicodes = new int[unicodes.Count];
                for (int i = 0; i < unicodes.Count; i++)
                {
                    _unicodes[i] = (int)unicodes[i];
                }
            }

            _lookup = null;
        }

        public bool Contains(uint unicode)
        {
            EnsureLookup();
            return _lookup.Contains((int)unicode);
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new HashSet<int>();
            if (_unicodes == null)
            {
                return;
            }

            for (int i = 0; i < _unicodes.Length; i++)
            {
                _lookup.Add(_unicodes[i]);
            }
        }

        private void OnEnable()
        {
            _lookup = null;
        }
    }
}
