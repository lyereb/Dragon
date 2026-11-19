using System.Reflection;
using Unity.Cinemachine;
using UnityEngine;

namespace Dragon.Logic
{
    public class CameraManager
    {
        private const string PlayerCameraObjectName = "PlayerCamera";
        private const string FollowAnchorObjectName = "PlayerCameraFollowAnchor";
        // 玩家与锚点在画面平面上的最大允许偏移（超出后才推动镜头）
        private const float FollowThresholdX = 2.5f;
        private const float FollowThresholdY = 2.5f;

        private CinemachineVirtualCameraBase _virtualCamera;
        private Transform _followAnchor;
        private Player _targetPlayer;
        private bool _hasBoundTarget = false;

        public void Initialize()
        {
            GameObject playerCameraObject = GameObject.Find(PlayerCameraObjectName);
            if (playerCameraObject == null)
            {
                Debug.LogWarning($"CameraManager.Initialize fail, can not find GameObject named '{PlayerCameraObjectName}' in scene.");
                return;
            }

            _virtualCamera = playerCameraObject.GetComponent<CinemachineVirtualCameraBase>();

            if (_virtualCamera == null)
            {
                Debug.LogWarning($"CameraManager.Initialize fail, '{PlayerCameraObjectName}' has no CinemachineVirtualCameraBase component.");
            }
        }

        public void BindFollowTarget(Player player)
        {
            if (player?.Skin == null)
            {
                Debug.LogWarning("CameraManager.BindFollowTarget fail, player or player skin is null.");
                return;
            }

            if (_virtualCamera == null)
            {
                Initialize();
                if (_virtualCamera == null)
                {
                    return;
                }
            }

            _targetPlayer = player;
            _followAnchor = GetOrCreateFollowAnchor(player.Skin.transform.position);
            _virtualCamera.Follow = _followAnchor;
            // 立刻移动虚拟相机到目标位置
            SetDampingValues(Vector3.zero);
            _virtualCamera.transform.position = _followAnchor.position;
            _hasBoundTarget = true;
        }

        public void LateUpdate(float dt)
        {
            if (!_hasBoundTarget || _targetPlayer?.Skin == null || _followAnchor == null)
            {
                return;
            }

            Vector3 playerPos = _targetPlayer.Skin.transform.position;
            Vector3 anchorPos = _followAnchor.position;

            float deltaX = playerPos.x - anchorPos.x;
            if (Mathf.Abs(deltaX) > FollowThresholdX)
            {
                anchorPos.x = playerPos.x - Mathf.Sign(deltaX) * FollowThresholdX;
            }

            float deltaY = playerPos.y - anchorPos.y;
            if (Mathf.Abs(deltaY) > FollowThresholdY)
            {
                anchorPos.y = playerPos.y - Mathf.Sign(deltaY) * FollowThresholdY;
            }

            _followAnchor.position = anchorPos;
        }

        public void Clear()
        {
            if (_virtualCamera != null)
            {
                _virtualCamera.Follow = null;
            }

            _virtualCamera = null;
            _targetPlayer = null;
            _followAnchor = null;
            _hasBoundTarget = false;
        }

        private static Transform GetOrCreateFollowAnchor(Vector3 initialPosition)
        {
            GameObject anchorObject = GameObject.Find(FollowAnchorObjectName);
            if (anchorObject == null)
            {
                anchorObject = new GameObject(FollowAnchorObjectName);
            }

            anchorObject.transform.position = initialPosition;

            return anchorObject.transform;
        }

        private void SetDampingValues(Vector3 damping)
        {
            if (_virtualCamera != null)
            {
                CinemachineComponentBase body = _virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body);
                if (body is CinemachinePositionComposer composer)
                {
                    composer.Damping = damping;
                    return;
                }

                if (body == null)
                {
                    return;
                }

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
                body.GetType().GetField("m_XDamping", flags)?.SetValue(body, damping.x);
                body.GetType().GetField("m_YDamping", flags)?.SetValue(body, damping.y);
                body.GetType().GetField("m_ZDamping", flags)?.SetValue(body, damping.z);
            }
        }
    }
}
