using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class EntitySpawnner
{
    private Transform SpawnPointRoot;

    public void Initialize(Transform spawnPointRoot)
    {
        SpawnPointRoot = spawnPointRoot;
    }

    public void OnFinishSpawn()
    {
        SpawnPointRoot.gameObject.SetActive(false);
    }

    public async UniTask<List<Entity>> SpawnAll()
    {
        List<Entity> spawnedEntities = new List<Entity>();
        if (SpawnPointRoot == null)
        {
            Debug.LogError("EntitySpawnner.SpawnAll fail, SpawnPointRoot is null.");
            return spawnedEntities;
        }

        SpawnPoint[] spawnPoints = SpawnPointRoot.GetComponentsInChildren<SpawnPoint>(true);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            SpawnPoint spawnPoint = spawnPoints[i];
            if (IsPlayerSpawnPoint(spawnPoint))
            {
                continue;
            }

            Entity entity = CreateEntityBySpawnPoint(spawnPoint);
            if (entity == null)
            {
                continue;
            }

            EntityInitContext initContext = CreateInitContextBySpawnPoint(spawnPoint);
            if (!await entity.Initialize(initContext))
            {
                Debug.LogError($"EntitySpawnner.SpawnAll initialize fail, point={spawnPoint.name}, configId={spawnPoint.ConfigId}");
                continue;
            }

            spawnedEntities.Add(entity);
        }
        return spawnedEntities;
    }

    public async UniTask<Player> SpawnPlayer()
    {
        if (SpawnPointRoot == null)
        {
            Debug.LogError("EntitySpawnner.SpawnPlayer fail, SpawnPointRoot is null.");
            return null;
        }

        SpawnPoint[] spawnPoints = SpawnPointRoot.GetComponentsInChildren<SpawnPoint>(true);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            SpawnPoint spawnPoint = spawnPoints[i];
            if (!IsPlayerSpawnPoint(spawnPoint))
            {
                continue;
            }

            Player player = new Player();
            EntityInitContext initContext = CreateInitContextBySpawnPoint(spawnPoint);
            if (!await player.Initialize(initContext))
            {
                Debug.LogError($"EntitySpawnner.SpawnPlayer initialize fail, point={spawnPoint.name}, configId={spawnPoint.ConfigId}");
                return null;
            }

            return player;
        }

        Debug.LogWarning("EntitySpawnner.SpawnPlayer can not find player SpawnPoint.");
        return null;
    }

    private static Entity CreateEntityBySpawnPoint(SpawnPoint spawnPoint)
    {
        switch (spawnPoint.EntityType)
        {
            case EntityType.Character:
                return CreateCharacterByType(spawnPoint.CharacterType);
            case EntityType.Interactable:
                return CreateInteractableByType(spawnPoint.InteractableType);
            default:
                Debug.LogWarning($"EntitySpawnner: unsupported entityType={spawnPoint.EntityType}, point={spawnPoint.name}");
                return null;
        }
    }

    private static Entity CreateCharacterByType(CharacterType characterType)
    {
        switch (characterType)
        {
            case CharacterType.Player:
                return null;
            case CharacterType.Enemy:
                return new Enemy();
            case CharacterType.NPC:
                return new Npc();
            default:
                Debug.LogWarning($"EntitySpawnner: unsupported characterType={characterType}");
                return new Character();
        }
    }

    private static Entity CreateInteractableByType(InteractableType interactableType)
    {
        switch (interactableType)
        {
            case InteractableType.Block:
                return new Block();
            default:
                Debug.LogWarning($"EntitySpawnner: unsupported interactableType={interactableType}");
                return new Interactable();
        }
    }

    private static EntityInitContext CreateInitContextBySpawnPoint(SpawnPoint spawnPoint)
    {
        EntityInitContext context;
        switch (spawnPoint.EntityType)
        {
            case EntityType.Character:
                switch (spawnPoint.CharacterType)
                {
                    case CharacterType.Player:
                        context = new PlayerInitContext();
                        break;
                    case CharacterType.Enemy:
                        context = new EnemyInitContext();
                        break;
                    case CharacterType.NPC:
                        context = new NpcInitContext();
                        break;
                    default:
                        context = new CharacterInitContext();
                        break;
                }
                break;
            case EntityType.Interactable:
                switch (spawnPoint.InteractableType)
                {
                    case InteractableType.Block:
                        context = new BlockInitContext();
                        break;
                    default:
                        context = new InteractableInitContext();
                        break;
                }
                break;
            default:
                context = new EntityInitContext();
                break;
        }

        context.SpawnLogicPosition = spawnPoint.SpawnLogicPosition;
        context.EntityConfigId = spawnPoint.ConfigId;
        return context;
    }

    private static bool IsPlayerSpawnPoint(SpawnPoint spawnPoint)
    {
        return spawnPoint.EntityType == EntityType.Character
               && spawnPoint.CharacterType == CharacterType.Player;
    }
}