using ArmouryWorldOfWarcraft.Runtime;
using System;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ArmouryWorldOfWarcraft.Editor
{
    internal static class FrostmourneProgressionGenerator
    {
        internal static readonly string[] WeaponGuids =
        {
            FrostmourneGenerator.WeaponGuid, "2f694b9b994a4db48f5981bdc710b682", "30cca993058149758fc0d641f34d2ee8",
            "eb5208c812c34015b23ce697d0eb74c1", "759a4159c89e4ccdaae2b416772d8880", "721d679addcc4e529b9950f1a97606a0"
        };
        private static readonly int[] DamageMin = { 16, 18, 25, 35, 38, 39 };
        private static readonly int[] DamageMax = { 23, 26, 38, 46, 56, 68 };
        private static readonly int[] Penetration = { 30, 35, 40, 45, 50, 60 };
        private static readonly string[] NameKeys = { "base", "taker", "hungering", "bane", "herald", "end" };

        internal const string SoulBuffGuid = "dbe5c6279f28408db73133a4320b20cb";
        internal const string SoulDisplayBuffGuid = "ac41878a90594f53af75e05fca334817";
        private const string StrikeGuid = "e92f69120a7d42cf9d5c392677c0bdf5";
        private const string CleaveGuid = "41ed610c9c2e4b69999de48979c98f78";
        private const string SoulrendGuid = "35b513974f244d57b72f21ec08f4f367";
        private const string ChainsGuid = "6f843da761ac439aa6d001640a5e203d";
        private const string HarvestGuid = "d7f4af60411147f5be160d6d69f55faf";
        private const string HarvestWeaponGuid = "bdad3e36af574d099161e1709dbebbc3";
        private const string SwordAttackFxGuid = "1bc92b9832fe402caa887d8c5d990cb4";

        private const string WeaponPrototype = "88863b6b0c61404b96b01c2bc648ba5e";
        private const string StrikePrototype = "9dec1bdade284190b0977f5f70d26d3e";
        private const string CleavePrototype = "163013a18e9c46419b2311454ad2b2c8";
        private const string SoulGazePrototype = "ec639338f11e49b1a75a7b9757a77076";
        private const string FreezePrototype = "9dec1bdade284190b0977f5f70d26d3e";
        private const string BuffPrototype = "60f2e3083d2d4498bfc755fe1618f78f";
        private const string OverrideWeaponReference = "84c32baad3f14585a32f5747d721dfc3";

        private const string Root = "Assets/Modifications/ArmouryWorldOfWarcraft";
        private const string Blueprints = Root + "/Blueprints";
        private const string Icons = Root + "/Art/Abilities";

        internal static void Generate()
        {
            foreach (string pattern in new[] { "Frostmourne_V*_Item.jbp", "Frostmourne_*_Ability.jbp", "Frostmourne_*_Buff.jbp", "Frostmourne_Hidden*.jbp" })
                foreach (string old in Directory.GetFiles(Blueprints, pattern)) File.Delete(old);

            var iconRefs = new[]
            {
                Icon("Frostmourne_Strike_Icon.png"), Icon("Frostmourne_Cleave_Icon.png"), Icon("Frostmourne_Soulrend_Icon.png"),
                Icon("Frostmourne_ChainsOfIce_Icon.png"), Icon("Frostmourne_HarvestSoul_Icon.png")
            };
            GenerateAbilities(iconRefs);
            GenerateSoulBuff(iconRefs[4]);

            string basePath = Path.Combine(Blueprints, "Frostmourne_Item.jbp");
            JObject source = JObject.Parse(File.ReadAllText(basePath));
            for (int i = 0; i < 6; i++)
            {
                int tier = i + 1;
                JObject weapon = PrepareClone((JObject)source.DeepClone(), WeaponGuids[i], WeaponPrototype);
                weapon["Data"]["Components"] = new JArray();
                SetLocalized(weapon, "m_DisplayName", $"wow-frostmourne-{NameKeys[i]}-name");
                SetLocalized(weapon, "m_Description", $"wow-frostmourne-{NameKeys[i]}-description");
                SetLocalized(weapon, "m_FlavorText", "wow-frostmourne-flavor");
                Override(weapon, "WarhammerDamage", DamageMin[i]);
                Override(weapon, "WarhammerMaxDamage", DamageMax[i]);
                Override(weapon, "WarhammerPenetration", Penetration[i]);
                Override(weapon, "ItemLevel", tier == 6 ? 55 : i * 10 + 9);
                Override(weapon, "m_Rarity", tier <= 2 ? "Pattern" : "Unique");
                Override(weapon, "m_Enchantments", new JArray());
                SetAbilitySlot(weapon, "Ability1", "SingleShot", StrikeGuid, 1);
                SetAbilitySlot(weapon, "Ability2", "AOE", CleaveGuid, 2);
                if (tier >= 2) SetAbilitySlot(weapon, "Ability3", "Custom", SoulrendGuid, 1); else ClearAbilitySlot(weapon, "Ability3");
                if (tier >= 4) SetAbilitySlot(weapon, "Ability4", "Custom", ChainsGuid, 2); else ClearAbilitySlot(weapon, "Ability4");
                if (tier >= 6) SetAbilitySlot(weapon, "Ability5", "SingleShot", HarvestGuid, 2); else ClearAbilitySlot(weapon, "Ability5");
                weapon["Data"]["m_AttackOfOpportunityAbility"] = "!bp_" + StrikeGuid;
                AddOverride(weapon, "m_AttackOfOpportunityAbility");
                Save($"Frostmourne_V{tier}_Item", weapon);
            }
            File.Delete(basePath);
        }

        private static void GenerateAbilities((string guid, long fileId)[] icons)
        {
            JObject strike = PrepareClone(Load(StrikePrototype), StrikeGuid, StrikePrototype);
            ConfigureAbility(strike, "wow-frostmourne-strike-name", "wow-frostmourne-strike-description", icons[0], 1);
            Save("Frostmourne_Strike_Ability", strike);

            JObject cleave = PrepareClone(Load(CleavePrototype), CleaveGuid, CleavePrototype);
            ConfigureAbility(cleave, "wow-frostmourne-cleave-name", "wow-frostmourne-cleave-description", icons[1], 2);
            Save("Frostmourne_Cleave_Ability", cleave);

            // Sentinel Sword Wave method: retain the normal melee attack delivery and merely
            // extend its targeting range. This keeps damage tied to the equipped Frostmourne.
            JObject soulrend = PrepareClone(Load(StrikePrototype), SoulrendGuid, StrikePrototype);
            RemoveUsageRestrictions(soulrend);
            JObject soulrendDelivery = (JObject)soulrend["Data"]["Components"].Children<JObject>()
                .First(c => c["$type"]?.ToString().Contains("WarhammerAbilityAttackDelivery") == true);
            soulrendDelivery["UseBestShootingPosition"] = false;
            JObject soulrendDodge = soulrend["Data"]["Components"].Children<JObject>()
                .SelectMany(component => component["Actions"]?["Actions"]?.Children<JObject>() ?? Enumerable.Empty<JObject>())
                .First(action => action["$type"]?.ToString().Contains("DodgeActions") == true);
            JObject purgeSoulHitFx = new JObject
            {
                ["$type"] = "120df4726e71c854e95f84b87a99a3c5, ContextActionSpawnFx",
                ["name"] = "$ContextActionSpawnFx$FrostmourneSoulrendPurgeSoulHit",
                ["PrefabLink"] = new JObject { ["AssetId"] = "46a393f9ec2eddf46a09d80881e74622" }
            };
            ((JArray)soulrendDodge["ActionsOnHit"]["Actions"]).Insert(0, purgeSoulHitFx);
            ConfigureAbility(soulrend, "wow-frostmourne-soulrend-name", "wow-frostmourne-soulrend-description", icons[2], 1);
            Override(soulrend, "Range", "Custom");
            Override(soulrend, "CustomRange", 8);
            Override(soulrend, "MinRange", 0);
            Override(soulrend, "UsingInThreateningArea", "CanUseWithoutAOO");
            Override(soulrend, "DisableBestShootingPosition", true);
            Override(soulrend, "NeedEquipWeapons", false);
            Override(soulrend, "Type", "Weapon");
            Override(soulrend, "AbilityParamsSource", "Weapon");
            Override(soulrend, "Animation", "Directional");
            Override(soulrend, "ShouldTurnToTarget", true);
            Override(soulrend, "m_FXSettings", "!bp_" + SwordAttackFxGuid);
            Save("Frostmourne_Soulrend_Ability", soulrend);

            JObject chains = PrepareClone(Load(FreezePrototype), ChainsGuid, FreezePrototype);
            RemoveUsageRestrictions(chains);
            ConfigureAbility(chains, "wow-frostmourne-chains-name", "wow-frostmourne-chains-description", icons[3], 2);
            Save("Frostmourne_ChainsOfIce_Ability", chains);

            GenerateHiddenHarvestWeapon();
            JObject harvest = PrepareClone(Load(StrikePrototype), HarvestGuid, StrikePrototype);
            ConfigureAbility(harvest, "wow-frostmourne-harvest-name", "wow-frostmourne-harvest-description", icons[4], 2);
            JObject component = (JObject)Load(OverrideWeaponReference)["Data"]["Components"].Children<JObject>()
                .First(c => c["$type"]?.ToString().Contains("WarhammerOverrideAbilityWeapon") == true).DeepClone();
            component["name"] = "$WarhammerOverrideAbilityWeapon$FrostmourneHarvest";
            component["PrototypeLink"] = new JObject { ["guid"] = "", ["name"] = "" };
            component["m_Weapon"] = "!bp_" + HarvestWeaponGuid;
            component["m_ForceShowWeaponDamageInUi"] = true;
            ((JArray)harvest["Data"]["Components"]).Add(component);
            AddOverride(harvest, component["name"].ToString());
            Override(harvest, "Type", "Weapon");
            Override(harvest, "AbilityParamsSource", "Weapon");
            Save("Frostmourne_HarvestSoul_Ability", harvest);
            JObject oneHandedHarvest = (JObject)harvest.DeepClone();
            oneHandedHarvest["AssetId"] = FrostmourneOneHandedBlueprints.HarvestAbilityGuid;
            JObject oneHandedOverride = oneHandedHarvest["Data"]["Components"].Children<JObject>()
                .Single(c => c["$type"]?.ToString().Contains("WarhammerOverrideAbilityWeapon") == true);
            oneHandedOverride["m_Weapon"] = "!bp_" + FrostmourneOneHandedBlueprints.HarvestWeaponGuid;
            Save("Frostmourne_OneHanded_HarvestSoul_Ability", oneHandedHarvest);
        }

        internal static int OneHandedDamage(int damage) => (int)Math.Round(damage * 0.75, MidpointRounding.AwayFromZero);

        private static void GenerateHiddenHarvestWeapon()
        {
            JObject hidden = PrepareClone(Load(WeaponPrototype), HarvestWeaponGuid, WeaponPrototype);
            hidden["Data"]["Components"] = new JArray();
            hidden["Data"]["m_VisualParameters"]["m_WeaponModel"] = null;
            AddOverride(hidden, "m_VisualParameters.m_WeaponModel");
            Override(hidden, "Classification", "None");
            Override(hidden, "WarhammerDamage", DamageMin[5] * 2);
            Override(hidden, "WarhammerMaxDamage", DamageMax[5] * 2);
            Override(hidden, "WarhammerPenetration", 100);
            Override(hidden, "CanBeUsedInGame", false);
            Override(hidden, "IsUnlootable", true);
            Save("Frostmourne_HiddenHarvest_Item", hidden);
            JObject oneHandedHidden = (JObject)hidden.DeepClone();
            oneHandedHidden["AssetId"] = FrostmourneOneHandedBlueprints.HarvestWeaponGuid;
            Override(oneHandedHidden, "WarhammerDamage", OneHandedDamage(DamageMin[5]) * 2);
            Override(oneHandedHidden, "WarhammerMaxDamage", OneHandedDamage(DamageMax[5]) * 2);
            Override(oneHandedHidden, "m_HoldingType", "OneHanded");
            Override(oneHandedHidden, "IsTwoHanded", false);
            Save("Frostmourne_HiddenOneHandedHarvest_Item", oneHandedHidden);
        }

        private static void GenerateSoulBuff((string guid, long fileId) icon)
        {
            // The game also removes buffs without StayOnDeath when their owner becomes unconscious.
            JObject storage = CreateSoulBuff(SoulBuffGuid, icon, "HiddenInUi, StayOnDeath");
            Save("Frostmourne_SoulsDevoured_Buff", storage);
            JObject display = CreateSoulBuff(SoulDisplayBuffGuid, icon, "None");
            Save("Frostmourne_SoulsDevoured_Display_Buff", display);
        }

        private static JObject CreateSoulBuff(string guid, (string guid, long fileId) icon, string flags)
        {
            JObject buff = PrepareClone(Load(BuffPrototype), guid, BuffPrototype);
            buff["Data"]["Components"] = new JArray();
            SetLocalized(buff, "m_DisplayName", "wow-frostmourne-souls-name");
            SetLocalized(buff, "m_Description", "wow-frostmourne-souls-description");
            SetIcon(buff, icon);
            Override(buff, "Ranks", 999);
            Override(buff, "m_Flags", flags);
            Override(buff, "IsImportantBuff", false);
            Override(buff, "IsClassFeature", false);
            return buff;
        }
        private static void RemoveUsageRestrictions(JObject ability)
        {
            JArray components = (JArray)ability["Data"]["Components"];
            foreach (JObject component in components.Children<JObject>().ToArray())
            {
                string type = component["$type"]?.ToString() ?? "";
                if (type.Contains("ResourceLogic") || type.Contains("CasterHasFact") || type.Contains("Prerequisite")) component.Remove();
            }
        }

        private static void ConfigureAbility(JObject ability, string name, string description, (string guid, long fileId) icon, int ap)
        {
            SetLocalized(ability, "m_DisplayName", name);
            SetLocalized(ability, "m_Description", description);
            SetIcon(ability, icon);
            Override(ability, "ActionPointCost", ap);
            Override(ability, "VeilThicknessPointsToAdd", 0);
            Override(ability, "CombatStateRestriction", "NoRestriction");
        }

        private static (string guid, long fileId) Icon(string file)
        {
            string path = Icons + "/" + file;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidDataException("Icon importer missing: " + path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
            UnityEngine.Object icon = AssetDatabase.LoadAllAssetsAtPath(path).FirstOrDefault(a => a is Sprite) ?? AssetDatabase.LoadMainAssetAtPath(path);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon, out string guid, out long fileId)) throw new InvalidDataException("Icon reference missing: " + path);
            return (guid, fileId);
        }

        private static JObject Load(string id)
        {
            BlueprintJsonWrapper wrapper = BlueprintsDatabase.LoadWrapperById(id);
            if (wrapper != null) { using var writer = new StringWriter(); Json.Serializer.Serialize(writer, wrapper); return JObject.Parse(writer.ToString()); }
            string path = id switch
            {
                WeaponPrototype => "Blueprints/Weapons/Weapons/GreatSword_Item.jbp",
                StrikePrototype => "Blueprints/Templates/Strike.jbp",
                CleavePrototype => "Blueprints/Templates/Cleave.jbp",
                OverrideWeaponReference => "Blueprints/Templates/OverrideWeapon.jbp",
                SoulGazePrototype => "Blueprints/Templates/ChargeAbility.jbp",
                BuffPrototype => "Blueprints/Templates/SoulCounterBuff.jbp",
                _ => null
            };
            if (path == null || !File.Exists(path)) throw new InvalidDataException("Blueprint not found: " + id);
            return JObject.Parse(File.ReadAllText(path));
        }

        private static JObject PrepareClone(JObject root, string id, string prototype)
        {
            root["AssetId"] = id; root["Data"]["PrototypeLink"] = prototype; root["Data"]["m_Overrides"] = new JArray();
            foreach (JObject component in root["Data"]["Components"].Children<JObject>())
            { component["PrototypeLink"] = new JObject { ["guid"] = prototype, ["name"] = component["name"]?.ToString() ?? "" }; component["m_Overrides"] = new JArray(); }
            return root;
        }

        private static void SetIcon(JObject root, (string guid, long fileId) icon)
        { root["Data"]["m_Icon"] = new JObject { ["guid"] = icon.guid, ["fileid"] = icon.fileId }; AddOverride(root, "m_Icon"); }
        private static void Save(string name, JObject root) => File.WriteAllText(Path.Combine(Blueprints, name + ".jbp"), root.ToString(Formatting.Indented));
        private static void SetAbilitySlot(JObject weapon, string slotName, string type, string ability, int ap)
        {
            JObject slot = (JObject)weapon["Data"]["AbilityContainer"][slotName];
            slot["Type"] = type; slot["Mode"] = "Default"; slot["m_Ability"] = "!bp_" + ability; slot["m_FXSettings"] = null;
            slot["OnHitOverrideType"] = "None"; slot["m_OnHitActions"] = null; slot["AP"] = ap;
            foreach (string p in new[] { "Type", "Mode", "m_Ability", "m_FXSettings", "OnHitOverrideType", "m_OnHitActions", "AP" }) AddOverride(weapon, $"WeaponAbilities.{slotName}.{p}");
        }
        private static void ClearAbilitySlot(JObject weapon, string slotName)
        {
            JObject slot = (JObject)weapon["Data"]["AbilityContainer"][slotName];
            slot["Type"] = "None"; slot["Mode"] = "Default"; slot["m_Ability"] = null; slot["m_FXSettings"] = null;
            slot["OnHitOverrideType"] = "None"; slot["m_OnHitActions"] = null; slot["AP"] = 0;
            foreach (string p in new[] { "Type", "Mode", "m_Ability", "m_FXSettings", "OnHitOverrideType", "m_OnHitActions", "AP" }) AddOverride(weapon, $"WeaponAbilities.{slotName}.{p}");
        }
        private static void SetLocalized(JObject root, string property, string key)
        { root["Data"][property] = new JObject { ["m_Key"] = key, ["m_OwnerString"] = "", ["m_OwnerPropertyPath"] = "", ["m_JsonPath"] = "", ["Shared"] = null }; AddOverride(root, property); }
        private static void Override(JObject root, string property, JToken value) { root["Data"][property] = value; AddOverride(root, property); }
        private static void AddOverride(JObject root, string property)
        { JArray overrides = (JArray)root["Data"]["m_Overrides"]; if (!overrides.Values<string>().Contains(property)) overrides.Add(property); }
    }
}

