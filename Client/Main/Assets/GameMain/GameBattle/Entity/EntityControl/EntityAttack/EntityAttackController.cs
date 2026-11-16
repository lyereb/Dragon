public class EntityAttackController
{
    public void Update(Entity entity, IInputProvider inputProvider, float dt)
    {
        if (entity == null || inputProvider == null || dt <= 0f)
        {
            return;
        }

        if (entity.BlocksAttack())
        {
            return;
        }

        bool isAttackPressed = inputProvider.GetAttackInput(entity);
        if (isAttackPressed)
        {
            entity.NotifyAttackInput();
        }
    }

    public void ClearState()
    {

    }
}
