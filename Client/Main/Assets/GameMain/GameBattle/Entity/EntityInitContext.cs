using UnityEngine;

public class EntityInitContext
{
    public Vector3 SpawnLogicPosition;
    public int EntityConfigId;
}

public class CharacterInitContext : EntityInitContext
{
   
}

public class PlayerInitContext : CharacterInitContext
{
    public object PlayerSaveData;
}

public class EnemyInitContext : CharacterInitContext
{

}

public class NpcInitContext : CharacterInitContext
{

}

public class InteractableInitContext : EntityInitContext
{

}

public class BlockInitContext : InteractableInitContext
{

}