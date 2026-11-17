using UnityEngine;

public class EntityOccupancyGizmoDrawer : MonoBehaviour
{
    [SerializeField] private Color _rigidbodyColor = new Color(1f, 0.35f, 0.25f, 1f);
    [SerializeField] private Color _nonRigidbodyColor = new Color(0.2f, 0.8f, 1f, 1f);
    [SerializeField] private float _minGizmoHeight = 0.1f;

    private EntityManager _entityManager;

    public void Bind(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    private void OnDrawGizmos()
    {
        if (_entityManager == null)
        {
            return;
        }

        var entities = _entityManager.Entities;
        if (entities == null)
        {
            return;
        }

        for (int i = 0; i < entities.Count; i++)
        {
            Entity entity = entities[i];
            if (entity == null || entity.Data == null || entity.Data.Position == null)
            {
                continue;
            }

            float width = Mathf.Max(0.01f, entity.Data.WidthSize);
            float depth = Mathf.Max(0.01f, entity.Data.DepthSize);
            Vector3 renderPosition = entity.Data.Position.GetRenderPosition();
            Vector3 center = new Vector3(renderPosition.x, renderPosition.y, renderPosition.z);
            Vector3 size = new Vector3(width, depth, Mathf.Max(0.01f, _minGizmoHeight));

            Color baseColor = entity.Data.isRigidbody ? _rigidbodyColor : _nonRigidbodyColor;
            Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.18f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = baseColor;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
