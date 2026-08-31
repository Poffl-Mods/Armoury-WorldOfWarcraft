using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem;
using Kingmaker.View.MapObjects;
using UnityEngine;

namespace ArmouryWorldOfWarcraft.Runtime
{
    internal sealed class WorldOfWarcraftArmouryCacheController : MonoBehaviour
    {
        private const string TargetAreaGuid = "8a2d1ed55f694366b2d512e122bd19a7";
        private const string CacheBlueprintGuid = "cba94710e96647d89c93ff9dc5c566d2";
        private static readonly Vector3 CachePosition = new Vector3(116.304f, 2.419f, -207.103f);
        private static readonly Quaternion CacheRotation = Quaternion.Euler(0f, 35f, 0f);
        private static readonly string[] ArmouryItemGuids =
        {
            // Add future Armoury: World of Warcraft V1 weapon blueprints here.
            "f4a9c1e2837b4d5e8a6f9012bc34de56"
        };
        private float m_NextCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void EnsureController()
        {
            if (FindFirstObjectByType<WorldOfWarcraftArmouryCacheController>() != null) return;
            var host = new GameObject("ArmouryWorldOfWarcraft_ArmouryCacheController");
            DontDestroyOnLoad(host);
            host.AddComponent<WorldOfWarcraftArmouryCacheController>();
        }

        private void Update()
        {
            if (Time.unscaledTime >= m_NextCheck)
            {
                m_NextCheck = Time.unscaledTime + 1f;
                TrySpawnCache();
            }
        }

        private static void TrySpawnCache()
        {
            Game game = Game.Instance;
            AreaPersistentState areaState = game?.LoadedAreaState;
            if (game?.CurrentlyLoadedArea == null || areaState == null ||
                !string.Equals(game.CurrentlyLoadedArea.AssetGuid.ToString(), TargetAreaGuid, StringComparison.OrdinalIgnoreCase)) return;
            BlueprintDynamicMapObject blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintDynamicMapObject>(CacheBlueprintGuid);
            if (blueprint == null) return;
            bool alreadyExists = areaState.AllEntityData.OfType<DynamicMapObjectView.EntityData>()
                .Any(entity => entity.Blueprint != null && entity.Blueprint.AssetGuid == blueprint.AssetGuid);
            if (alreadyExists) return;
            SceneEntitiesState state = FindLoadedSceneState(areaState) ?? areaState.MainState;
            DynamicMapObjectView.EntityData entity = game.EntitySpawner.SpawnMapObject(blueprint, CachePosition, CacheRotation, state);
            InteractionLootPart loot = entity?.GetOptional<InteractionLootPart>();
            if (loot == null)
            {
                Debug.LogError("[ArmouryWorldOfWarcraft] World of Warcraft armoury cache spawned without InteractionLootPart.");
                return;
            }
            foreach (string guid in ArmouryItemGuids)
            {
                BlueprintItem item = ResourcesLibrary.TryGetBlueprint<BlueprintItem>(guid);
                if (item != null) loot.Loot.Add(item);
            }
            Debug.Log("[ArmouryWorldOfWarcraft] Persistent World of Warcraft armoury cache spawned and filled in the Warrant Chamber.");
        }

        private static SceneEntitiesState FindLoadedSceneState(AreaPersistentState areaState)
        {
            foreach (SceneEntitiesState state in areaState.GetAllSceneStates())
                if (state != null && state.IsSceneLoaded && state.SceneName.StartsWith("VoidshipOfficersDeck", StringComparison.OrdinalIgnoreCase))
                    return state;
            return null;
        }

    }
}
