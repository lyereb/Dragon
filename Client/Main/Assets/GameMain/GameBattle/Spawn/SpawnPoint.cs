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
    /// renderY = logicY + logicZ * DepthToVerticalOffset
    /// </summary>
    public Vector3 SpawnLogicPosition
    {
        get
        {
            Vector3 renderPos = transform.position;
            float logicX = renderPos.x;
            float logicZ = renderPos.z;
            float logicY = renderPos.y - logicZ * GlobalDefine.DepthToVerticalOffset;
            return new Vector3(logicX, logicY, logicZ);
        }
    }
}
