using UnityEngine;

public class Interactable : Entity
{
    public new InteractableData Data => GetData<InteractableData>();

    protected override EntityData CreateEntityData(EntityInitContext context)
    {
        InteractableData data = new InteractableData();
        ApplyEntityData(data, context);
        return data;
    }

    protected override void ApplyEntityData(EntityData data, EntityInitContext context)
    {
        base.ApplyEntityData(data, context);
        if (data is InteractableData interactableData && context is InteractableInitContext interactableContext)
        {
            ApplyInteractableData(interactableData, interactableContext);
        }
    }

    protected virtual void ApplyInteractableData(InteractableData data, InteractableInitContext context)
    {
        
    }

    protected override EntityType GetEntityType()
    {
        return EntityType.Interactable;
    }

    protected virtual InteractableType GetInteractableType()
    {
        return default;
    }

    protected override bool OnFSMSetup()
    {
        FSM.Register(new InteractableBirthState());
        FSM.Register(new InteractableIdleState());
        FSM.Register(new InteractableDeathState());
        return true;
    }

    protected override bool OnInitAfter()
    {
        return true;
    }

    protected override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);
    }

    protected override void OnLateUpdate(float dt)
    {
        base.OnLateUpdate(dt);
    }
}
