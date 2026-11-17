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
    /// 按当前 Transform 的画面坐标反算逻辑坐标。
    /// 俯视：画面 X/Y 就是逻辑 X/Y，高度为 0。
    /// </summary>
    public Vector3 SpawnLogicPosition
    {
        get
        {
            Vector3 renderPos = transform.position;
            return new Vector3(renderPos.x, renderPos.y, 0f);
        }
    }
}
