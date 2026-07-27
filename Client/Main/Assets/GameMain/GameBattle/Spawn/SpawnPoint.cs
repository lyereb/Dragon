using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private EntityType _entityType = EntityType.Character;
    [SerializeField] private CharacterType _characterType = CharacterType.Player;
    [SerializeField] private InteractableType _interactableType = InteractableType.Block;
    [SerializeField] private int _configId = 1;

    public EntityType EntityType => _entityType;
    public CharacterType CharacterType => _characterType;
    public InteractableType InteractableType => _interactableType;
    public int ConfigId => _configId;

    /// <summary>
    /// 按当前 Transform 的渲染坐标反算逻辑坐标。
    /// Logic: X = horizontal, Y = depth, Z = height.
    /// renderY = logicZ + logicY * DepthToVerticalOffset
    /// renderZ = logicY
    /// </summary>
    public Vector3 SpawnLogicPosition
    {
        get
        {
            Vector3 renderPos = transform.position;
            float logicX = renderPos.x;
            float logicY = renderPos.z;
            float logicZ = renderPos.y - logicY * GlobalDefine.DepthToVerticalOffset;
            return new Vector3(logicX, logicY, logicZ);
        }
    }
}
