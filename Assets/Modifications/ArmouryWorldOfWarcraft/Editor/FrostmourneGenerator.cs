using System;
using System.IO;
using System.Linq;
using Kingmaker.View.Equipment;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OwlcatModification.Editor;
using OwlcatModification.Editor.Build;
using UnityEditor;
using UnityEngine;

namespace ArmouryWorldOfWarcraft.Editor
{
    internal static class FrostmourneGenerator
    {
        internal const string WeaponGuid = "f4a9c1e2837b4d5e8a6f9012bc34de56";
        private const string WeaponPrototype = "88863b6b0c61404b96b01c2bc648ba5e";
        private const string Root = "Assets/Modifications/ArmouryWorldOfWarcraft";
        private const string Art = Root + "/Art";
        private const string Blueprints = Root + "/Blueprints";
        private const string FbxPath = Art + "/Frostmourne.fbx";
        private const string BaseColorPath = Art + "/Frostmourne_BaseColor.png";
        private const string MetallicPath = Art + "/Frostmourne_Metallic.png";
        private const string NormalPath = Art + "/Frostmourne_Normal.png";
        private const string RoughnessPath = Art + "/Frostmourne_Roughness.png";
        private const string IconPath = Art + "/Frostmourne_Icon.png";
        private const string MaterialPath = Art + "/Frostmourne.mat";
        private const string MaskPath = Art + "/Frostmourne_MetallicSmoothness.asset";
        private const string PackedNormalPath = Art + "/Frostmourne_NormalPacked.asset";
        private const string PrefabPath = Art + "/Frostmourne.prefab";
        private const string BeltPrefabPath = Art + "/Frostmourne_Holstered.prefab";

        private const string Version = "0.1.10";

        [MenuItem("Armoury World of Warcraft/Build 0.1.0")]
        public static void Build()
        {
            Generate();
            Modification mod = AssetDatabase.LoadAssetAtPath<Modification>(Root + "/ArmouryWorldOfWarcraft.asset");
            if (mod == null) throw new InvalidOperationException("Modification asset was not found.");
            mod.Manifest.Version = Version;
            EditorUtility.SetDirty(mod);
            AssetDatabase.SaveAssets();
            var result = Builder.Build(mod);
            if ((int)result != 0) throw new InvalidOperationException("Build failed: " + result);
        }

        [MenuItem("Armoury World of Warcraft/Generate Frostmourne")]
        public static void Generate()
        {
            Directory.CreateDirectory(Art);
            Directory.CreateDirectory(Blueprints);
            GenerateArt();
            GenerateBlueprint();
            FrostmourneProgressionGenerator.Generate();
            WorldOfWarcraftArmouryCacheGenerator.Generate();
            AssetDatabase.Refresh();
            Debug.Log("[ArmouryWorldOfWarcraft] Generated Frostmourne: " + WeaponGuid);
        }

        private static void GenerateArt()
        {
            AssetDatabase.ImportAsset(FbxPath, ImportAssetOptions.ForceSynchronousImport);
            ModelImporter importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null) throw new InvalidDataException("Frostmourne.fbx could not be imported.");
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.SaveAndReimport();

            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbx == null) throw new InvalidDataException("Imported Frostmourne FBX is missing.");
            Material material = CreateMaterial();
            GameObject root = new GameObject("Frostmourne_Root");
            EquipmentOffsets offsets = root.AddComponent<EquipmentOffsets>();
            ConfigureOffsets(offsets);
            AddOptionalComponent(root, "FxLocatorMapper");
            AddRequiredComponent(root, "ArmouryWorldOfWarcraft.Runtime.FrostmourneMaterialBinder");

            GameObject model = UnityEngine.Object.Instantiate(fbx);
            model.name = "Frostmourne_FBX_Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            // Point the blade down, with the skull/guard end towards the hands.
            model.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            model.transform.localScale = Vector3.one * 0.9f;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;

            Bounds bounds = CalculateBounds(model);
            // v0.0.7 paper-doll calibration: move another 1.2 metres in the same
            // direction as the v0.0.6 correction (offset +0.35 -> +1.55).
            Vector3 gripPosition = new Vector3(-bounds.center.x, -(bounds.max.y - 0.16f) + 1.25f, -bounds.center.z);
            model.transform.localPosition = gripPosition;
            Debug.Log($"[ArmouryWorldOfWarcraft] FBX bounds {bounds.size}; model offset {model.transform.localPosition}");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            // Holstered weapons use a separate visual so their pose cannot affect the
            // inventory or drawn weapon model.
            GameObject beltRoot = new GameObject("Frostmourne_Holstered_Root");
            EquipmentOffsets beltOffsets = beltRoot.AddComponent<EquipmentOffsets>();
            ConfigureOffsets(beltOffsets);
            AddOptionalComponent(beltRoot, "FxLocatorMapper");
            AddRequiredComponent(beltRoot, "ArmouryWorldOfWarcraft.Runtime.FrostmourneMaterialBinder");

            GameObject beltModel = UnityEngine.Object.Instantiate(fbx);
            beltModel.name = "Frostmourne_Holstered_FBX_Model";
            beltModel.transform.SetParent(beltRoot.transform, false);
            beltModel.transform.localPosition = gripPosition;
            beltModel.transform.localRotation = Quaternion.AngleAxis(90f, Vector3.up)
                * Quaternion.Euler(0f, 0f, 90f)
                * Quaternion.AngleAxis(90f, Vector3.right);
            beltModel.transform.localScale = Vector3.one * 0.9f;
            foreach (Renderer renderer in beltModel.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;

            PrefabUtility.SaveAsPrefabAsset(beltRoot, BeltPrefabPath);
            UnityEngine.Object.DestroyImmediate(beltRoot);
            AssetDatabase.SaveAssets();
        }

        private static Bounds CalculateBounds(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidDataException("The FBX contains no renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void ConfigureOffsets(EquipmentOffsets offsets)
        {
            SerializedObject serialized = new SerializedObject(offsets);
            SerializedProperty slots = serialized.FindProperty("m_SlotOffsets");
            slots.arraySize = 12;
            for (int i = 0; i < slots.arraySize; i++)
            {
                SerializedProperty slot = slots.GetArrayElementAtIndex(i);
                slot.FindPropertyRelative("Position").vector3Value = Vector3.zero;
                slot.FindPropertyRelative("Rotation").vector3Value = Vector3.zero;
            }
            SetSlotOffset(slots, 6, new Vector3(0.01f, -0.03f, -0.12f), new Vector3(358.31f, 95.50f, 90.41f));
            SetSlotOffset(slots, 8, new Vector3(-0.06f, -0.04f, -0.09f), new Vector3(0.91f, 281.02f, 276.02f));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSlotOffset(SerializedProperty slots, int index, Vector3 position, Vector3 rotation)
        {
            SerializedProperty slot = slots.GetArrayElementAtIndex(index);
            slot.FindPropertyRelative("Position").vector3Value = position;
            slot.FindPropertyRelative("Rotation").vector3Value = rotation;
        }

        private static void AddOptionalComponent(GameObject host, string typeName)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);
            if (type != null) host.AddComponent(type);
        }

        private static void AddRequiredComponent(GameObject host, string typeName)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);
            if (type == null) throw new InvalidOperationException(typeName + " was not found.");
            host.AddComponent(type);
        }

        private static Material CreateMaterial()
        {
            AssetDatabase.ImportAsset(BaseColorPath, ImportAssetOptions.ForceSynchronousImport);
            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath);
            Texture2D metallic = LoadPng(MetallicPath, true);
            Texture2D roughness = LoadPng(RoughnessPath, true);
            Texture2D normalSource = LoadPng(NormalPath, true);
            Texture2D mask = PackMask(metallic, roughness);
            Texture2D normal = PackNormal(normalSource);
            Shader shader = Shader.Find("Owlcat/Lit");
            if (shader == null) throw new InvalidOperationException("Owlcat/Lit shader is unavailable.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Frostmourne" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else material.shader = shader;
            SetTexture(material, baseColor, "_BaseMap", "_BaseColorMap", "_MainTex");
            SetTexture(material, mask, "_MetallicGlossMap", "_MaskMap", "_MasksMap");
            SetTexture(material, normal, "_BumpMap", "_NormalMap");
            SetFloat(material, 1f, "_Metallic");
            SetFloat(material, 1f, "_Smoothness");
            SetFloat(material, 0.4f, "_Roughness");
            SetColor(material, Color.white, "_BaseColor", "_Color", "_AdditionalAlbedoColor");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Texture2D LoadPng(string path, bool linear)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear);
            if (!texture.LoadImage(File.ReadAllBytes(path), false)) throw new InvalidDataException("Could not decode " + path);
            return texture;
        }

        private static Texture2D PackMask(Texture2D metallic, Texture2D roughness)
        {
            Color32[] metal = metallic.GetPixels32();
            Color32[] rough = roughness.GetPixels32();
            if (metal.Length != rough.Length) throw new InvalidDataException("Metallic and roughness sizes differ.");
            // Retain the authored roughness variation at the requested 0.4 strength.
            for (int i = 0; i < metal.Length; i++)
            {
                byte reducedRoughness = (byte)Mathf.RoundToInt(rough[i].r * 0.4f);
                metal[i] = new Color32(metal[i].r, 0, 0, (byte)(255 - reducedRoughness));
            }
            Texture2D packed = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, true, true) { name = "Frostmourne_MetallicSmoothness" };
            packed.SetPixels32(metal); packed.Apply(true, false);
            return ReplaceTexture(packed, MaskPath);
        }

        private static Texture2D PackNormal(Texture2D source)
        {
            Color32[] pixels = source.GetPixels32();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, pixels[i].g, 255, pixels[i].r);
            Texture2D packed = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true, true) { name = "Frostmourne_NormalPacked" };
            packed.SetPixels32(pixels); packed.Apply(true, false);
            return ReplaceTexture(packed, PackedNormalPath);
        }

        private static Texture2D ReplaceTexture(Texture2D source, string path)
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing == null) { AssetDatabase.CreateAsset(source, path); return source; }
            EditorUtility.CopySerialized(source, existing);
            UnityEngine.Object.DestroyImmediate(source);
            return existing;
        }

        private static void SetTexture(Material m, Texture t, params string[] names) { foreach (string n in names) if (m.HasProperty(n)) m.SetTexture(n, t); }
        private static void SetFloat(Material m, float v, params string[] names) { foreach (string n in names) if (m.HasProperty(n)) m.SetFloat(n, v); }
        private static void SetColor(Material m, Color v, params string[] names) { foreach (string n in names) if (m.HasProperty(n)) m.SetColor(n, v); }

        private static void GenerateBlueprint()
        {
            UnityEngine.Object icon = PrepareIcon(IconPath);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string iconGuid, out long iconFileId))
                throw new InvalidDataException("Frostmourne icon reference could not be resolved.");
            UnityEngine.Object prefab = AssetDatabase.LoadMainAssetAtPath(PrefabPath);
            if (prefab == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string prefabGuid, out long prefabFileId))
                throw new InvalidDataException("Prefab reference could not be resolved.");
            UnityEngine.Object beltPrefab = AssetDatabase.LoadMainAssetAtPath(BeltPrefabPath);
            if (beltPrefab == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(beltPrefab, out string beltPrefabGuid, out long beltPrefabFileId))
                throw new InvalidDataException("Holstered prefab reference could not be resolved.");
            JObject weapon = LoadPrototype();
            weapon["AssetId"] = WeaponGuid;
            weapon["Data"]["PrototypeLink"] = WeaponPrototype;
            weapon["Data"]["m_Overrides"] = new JArray();
            weapon["Data"]["Components"] = new JArray();
            SetLocalized(weapon, "m_DisplayName", "wow-frostmourne-name");
            SetLocalized(weapon, "m_Description", "wow-frostmourne-description");
            SetLocalized(weapon, "m_FlavorText", "wow-frostmourne-flavor");
            weapon["Data"]["m_Icon"] = new JObject { ["guid"] = iconGuid, ["fileid"] = iconFileId };
            AddOverride(weapon, "m_Icon");
            weapon["Data"]["m_VisualParameters"]["m_WeaponModel"] = new JObject { ["guid"] = prefabGuid, ["fileid"] = prefabFileId };
            AddOverride(weapon, "m_VisualParameters.m_WeaponModel");
            weapon["Data"]["m_VisualParameters"]["m_WeaponBeltModelOverride"] = new JObject { ["guid"] = beltPrefabGuid, ["fileid"] = beltPrefabFileId };
            AddOverride(weapon, "m_VisualParameters.m_WeaponBeltModelOverride");
            // Rogue Trader's serialized name for the TwoHandedBrutal animation set.
            weapon["Data"]["m_VisualParameters"]["m_WeaponAnimationStyle"] = "BrutalTwoHanded";
            AddOverride(weapon, "m_VisualParameters.m_WeaponAnimationStyle");
            Override(weapon, "Family", "Power");
            Override(weapon, "Classification", "Sword");
            Override(weapon, "m_HoldingType", "TwoHanded");
            Override(weapon, "IsTwoHanded", true);
            Override(weapon, "CanBeUsedInGame", true);
            Override(weapon, "IsUnlootable", false);
            Override(weapon, "IsNonRemovable", false);
            Override(weapon, "m_IsNotable", true);
            File.WriteAllText(Path.Combine(Blueprints, "Frostmourne_Item.jbp"), weapon.ToString(Formatting.Indented));
        }

        private static UnityEngine.Object PrepareIcon(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidDataException("Frostmourne icon importer was not found.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).FirstOrDefault(asset => asset is Sprite)
                ?? AssetDatabase.LoadMainAssetAtPath(path);
        }

        private static JObject LoadPrototype()
        {
            const string source = "Blueprints/Weapons/Weapons/GreatSword_Item.jbp";
            if (!File.Exists(source)) throw new FileNotFoundException("Vanilla GreatSword blueprint is missing.", source);
            return JObject.Parse(File.ReadAllText(source));
        }

        private static void SetLocalized(JObject root, string property, string key)
        {
            root["Data"][property] = new JObject { ["m_Key"] = key, ["m_OwnerString"] = "", ["m_OwnerPropertyPath"] = "", ["m_JsonPath"] = "", ["Shared"] = null };
            AddOverride(root, property);
        }

        private static void Override(JObject root, string property, JToken value) { root["Data"][property] = value; AddOverride(root, property); }
        private static void AddOverride(JObject root, string property)
        {
            JArray overrides = (JArray)root["Data"]["m_Overrides"];
            if (!overrides.Values<string>().Contains(property)) overrides.Add(property);
        }
    }
}