using UnityEngine;

public class EntityAttackController
{
    public void Update(Entity entity, IInputProvider inputProvider, float dt)
    {
        if (entity == null || inputProvider == null || dt <= 0f)
        {
            return;
        }

        bool isAttackPressed = inputProvider.GetAttackInput(entity);
        if (isAttackPressed)
        {
            //TODO: 执行攻击逻辑
            Debug.Log("Attack");
        }
    }

    public void ClearState()
    {

    }
}
