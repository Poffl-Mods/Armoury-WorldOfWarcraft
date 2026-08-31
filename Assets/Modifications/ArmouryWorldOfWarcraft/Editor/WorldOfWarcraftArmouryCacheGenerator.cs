using System.IO;
using Kingmaker.View.MapObjects;
using Kingmaker.View.MapObjects.InteractionComponentBase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ArmouryWorldOfWarcraft.Editor
{
    internal static class WorldOfWarcraftArmouryCacheGenerator
    {
        internal const string BlueprintGuid = "cba94710e96647d89c93ff9dc5c566d2";
        private const string PrefabPath = "Assets/Modifications/ArmouryWorldOfWarcraft/Art/WorldOfWarcraftArmouryCache.prefab";
        private const string BlueprintPath = "Assets/Modifications/ArmouryWorldOfWarcraft/Blueprints/WorldOfWarcraftArmouryCache_MapObject.jbp";

        internal static void Generate()
        {
            GameObject root = new GameObject("WorldOfWarcraftArmouryCache");
            root.SetActive(false);
            DynamicMapObjectView view = root.AddComponent<DynamicMapObjectView>();
            view.UniqueViewId = "armoury-world-of-warcraft-warrant-cache";
            // Owlcat strips shader programs from mod bundles. Rebind the bundled material
            // instances to the game's already-loaded Owlcat/Lit shader when the view awakens.
            System.Type materialBinderType = System.Type.GetType(
                "ArmouryWorldOfWarcraft.Runtime.FrostmourneMaterialBinder, ArmouryWorldOfWarcraft.Runtime");
            if (materialBinderType == null)
                throw new System.InvalidOperationException("World of Warcraft runtime material binder type was not found.");
            root.AddComponent(materialBinderType);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.48f, 0f);
            collider.size = new Vector3(1.75f, 1.05f, 1f);
            InteractionLoot interaction = root.AddComponent<InteractionLoot>();
            interaction.Settings = new InteractionLootSettings
            {
                Type = InteractionType.Approach,
                ShowOvertip = true, ShowHighlight = true, ProximityRadius = 2, NotInCombat = true,
                LootContainerType = LootContainerType.Chest, DestroyWhenEmpty = false,
                ShowOnMapWhenEmpty = false, AddMapMarker = false
            };
            // Reuse already proven weapon materials. Creating a fresh Owlcat/Lit material here can
            // leave its runtime shader variant outside the bundle and produces the magenta fallback.
            Material black = AssetDatabase.LoadAssetAtPath<Material>("Assets/Modifications/ArmouryWorldOfWarcraft/Art/Frostmourne.mat");
            Material gold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Modifications/ArmouryWorldOfWarcraft/Art/Frostmourne.mat");
            if (black == null || gold == null) throw new FileNotFoundException("World of Warcraft cache source material is missing.");
            Box(root.transform, "Vault body", new Vector3(0, .42f, 0), new Vector3(1.55f, .72f, .82f), black);
            Box(root.transform, "Vault lid", new Vector3(0, .84f, -.01f), new Vector3(1.62f, .18f, .86f), black);
            Box(root.transform, "Gold rim front", new Vector3(0, .54f, -.425f), new Vector3(1.62f, .09f, .055f), gold);
            Box(root.transform, "Gold rim rear", new Vector3(0, .54f, .425f), new Vector3(1.62f, .09f, .055f), gold);
            Box(root.transform, "Gold rim left", new Vector3(-.785f, .54f, 0), new Vector3(.055f, .09f, .8f), gold);
            Box(root.transform, "Gold rim right", new Vector3(.785f, .54f, 0), new Vector3(.055f, .09f, .8f), gold);
            Box(root.transform, "Lock", new Vector3(0, .58f, -.46f), new Vector3(.27f, .34f, .09f), gold);
            Box(root.transform, "Left brace", new Vector3(-.55f, .43f, -.445f), new Vector3(.09f, .68f, .07f), gold);
            Box(root.transform, "Right brace", new Vector3(.55f, .43f, -.445f), new Vector3(.09f, .68f, .07f), gold);
            root.SetActive(true);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string prefabGuid, out long fileId);
            JObject bp = new JObject
            {
                ["AssetId"] = BlueprintGuid,
                ["Data"] = new JObject
                {
                    ["$type"] = "b7d695389bff6604ca37f190a61f05b5, BlueprintDynamicMapObject",
                    ["PrototypeLink"] = "", ["m_Overrides"] = new JArray(), ["Components"] = new JArray(),
                    ["Author"] = "Poffl", ["Comment"] = "Persistent World of Warcraft armoury cache in the Warrant Chamber.",
                    ["m_DisplayName"] = null, ["m_Description"] = null, ["m_Icon"] = null,
                    ["Prefab"] = new JObject { ["guid"] = prefabGuid, ["fileid"] = fileId }
                },
                ["Meta"] = new JObject { ["ShadowDeleted"] = false }
            };
            File.WriteAllText(BlueprintPath, bp.ToString(Formatting.Indented));
            AssetDatabase.ImportAsset(BlueprintPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name; box.transform.SetParent(parent, false); box.transform.localPosition = position; box.transform.localScale = scale;
            Object.DestroyImmediate(box.GetComponent<Collider>()); box.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
