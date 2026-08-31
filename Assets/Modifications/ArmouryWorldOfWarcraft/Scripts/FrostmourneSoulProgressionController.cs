using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Mechanics.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using UnityEngine;

namespace ArmouryWorldOfWarcraft.Runtime
{
    public sealed class FrostmourneSoulProgressionController : MonoBehaviour,
        IDamageHandler, IUnitDeathHandler, IItemsCollectionHandler, IPartyCombatHandler
    {
        private static readonly string[] WeaponGuids =
        {
            "f4a9c1e2837b4d5e8a6f9012bc34de56", "2f694b9b994a4db48f5981bdc710b682", "30cca993058149758fc0d641f34d2ee8",
            "eb5208c812c34015b23ce697d0eb74c1", "759a4159c89e4ccdaae2b416772d8880", "721d679addcc4e529b9950f1a97606a0",
            "bdad3e36af574d099161e1709dbebbc3"
        };
        private static readonly string[] FrostmourneAbilityGuids =
        {
            "e92f69120a7d42cf9d5c392677c0bdf5", "41ed610c9c2e4b69999de48979c98f78", "35b513974f244d57b72f21ec08f4f367",
            "6f843da761ac439aa6d001640a5e203d", "d7f4af60411147f5be160d6d69f55faf"
        };
        private const string SoulBuffGuid = "dbe5c6279f28408db73133a4320b20cb";
        private const string ChainsAbilityGuid = "6f843da761ac439aa6d001640a5e203d";
        private const string ImmobilizedBuffGuid = "3f47d39ccc2b4104bbf6c471c693bfa8";
        private static readonly MethodInfo SubscribeGlobalMethod = typeof(EventBus).GetMethod("SubscribeGlobal", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo UnsubscribeGlobalMethod = typeof(EventBus).GetMethod("UnsubscribeGlobal", BindingFlags.Static | BindingFlags.NonPublic);
        private readonly Dictionary<AbstractUnitEntity, BaseUnitEntity> m_Marked = new Dictionary<AbstractUnitEntity, BaseUnitEntity>();
        private readonly HashSet<ItemsCollection> m_KnownCollections = new HashSet<ItemsCollection>();
        private float m_NextCheck;
        private bool m_ChangingItems;
        private bool m_DeferredByCombat;
        private bool m_ShowSoulTestPanel;
        private Rect m_SoulTestWindow = new Rect(20f, 120f, 310f, 245f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureController()
        {
            if (FindFirstObjectByType<FrostmourneSoulProgressionController>() != null) return;
            GameObject host = new GameObject("ArmouryWorldOfWarcraft_FrostmourneSoulProgression");
            DontDestroyOnLoad(host);
            host.AddComponent<FrostmourneSoulProgressionController>();
        }

        private void OnEnable() => SubscribeGlobalMethod?.Invoke(null, new object[] { this, null });
        private void OnDisable() => UnsubscribeGlobalMethod?.Invoke(null, new object[] { this, null });

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8)) m_ShowSoulTestPanel = !m_ShowSoulTestPanel;
            if (Time.unscaledTime < m_NextCheck) return;
            m_NextCheck = Time.unscaledTime + 1f;
            RememberPlayerCollections();
            TryUpgradeAll();
        }

        private void OnGUI()
        {
            if (!m_ShowSoulTestPanel) return;
            m_SoulTestWindow = GUI.Window(GetInstanceID(), m_SoulTestWindow, DrawSoulTestWindow, "Frostmourne Soul Test");
        }

        private void DrawSoulTestWindow(int windowId)
        {
            int souls = GetPartySoulCount();
            GUILayout.Label($"Souls Devoured: {souls}/150");
            GUILayout.Label(IsCombatActive(Game.Instance?.Player) ? "Weapon upgrades wait until combat ends." : "Weapon upgrades apply immediately.");
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1")) AddTestSouls(1);
            if (GUILayout.Button("+10")) AddTestSouls(10);
            GUILayout.EndHorizontal();
            GUILayout.Label("Advance to awakening threshold:");
            GUILayout.BeginHorizontal();
            foreach (int threshold in new[] { 30, 60, 90 })
                if (GUILayout.Button(threshold.ToString())) SetTestSoulMinimum(threshold);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            foreach (int threshold in new[] { 120, 150 })
                if (GUILayout.Button(threshold.ToString())) SetTestSoulMinimum(threshold);
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            GUILayout.Label("F8 closes this test window.");
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void AddTestSouls(int amount) => SetTestSoulMinimum(Math.Min(150, GetPartySoulCount() + amount));

        private void SetTestSoulMinimum(int target)
        {
            Player player = Game.Instance?.Player;
            BaseUnitEntity bearer = player == null ? null : EnumeratePartyUnits(player)
                .OrderByDescending(GetSoulCount).FirstOrDefault();
            if (bearer == null)
            {
                Debug.LogError("[ArmouryWorldOfWarcraft] No party member was found for the soul test.");
                return;
            }
            SetSoulCount(bearer, Math.Max(GetPartySoulCount(), target));
            Debug.Log($"[ArmouryWorldOfWarcraft] Soul test set Souls Devoured to {GetPartySoulCount()}/150.");
            TryUpgradeAll();
        }

        public void HandleDamageDealt(RuleDealDamage dealDamage)
        {
            if (dealDamage == null || dealDamage.Result <= 0) return;
            string contextWeaponGuid = dealDamage.ContextDamageWeapon?.AssetGuid.ToString();
            string reasonItemGuid = dealDamage.Reason.Item?.Blueprint?.AssetGuid.ToString();
            string sourceAbilityGuid = dealDamage.SourceAbility?.Blueprint?.AssetGuid.ToString()
                ?? dealDamage.Reason.Ability?.Blueprint?.AssetGuid.ToString();
            bool isFrostmourneDamage = Array.IndexOf(WeaponGuids, contextWeaponGuid) >= 0
                || Array.IndexOf(WeaponGuids, reasonItemGuid) >= 0
                || Array.IndexOf(FrostmourneAbilityGuids, sourceAbilityGuid) >= 0;
            if (!isFrostmourneDamage) return;
            BaseUnitEntity target = dealDamage.TargetUnit;
            BaseUnitEntity bearer = dealDamage.InitiatorUnit;
            if (target == null || bearer == null || ReferenceEquals(target, bearer)) return;
            m_Marked[target] = bearer;
            Debug.Log($"[ArmouryWorldOfWarcraft] Frostmourne marked {target.CharacterName} for soul harvest.");
            if (sourceAbilityGuid == ChainsAbilityGuid)
            {
                BlueprintBuff immobilized = ResourcesLibrary.TryGetBlueprint(ImmobilizedBuffGuid) as BlueprintBuff;
                if (immobilized != null)
                    target.Buffs.Add(immobilized, bearer, new BuffDuration(null, BuffEndCondition.TurnEndOrCombatEnd));
            }
        }


        public void HandleUnitDeath(AbstractUnitEntity unitEntity)
        {
            if (unitEntity == null || !m_Marked.TryGetValue(unitEntity, out BaseUnitEntity bearer)) return;
            m_Marked.Remove(unitEntity);
            int next = Math.Min(150, Math.Max(GetPartySoulCount(), GetSoulCount(bearer)) + 1);
            SetSoulCount(bearer, next);
            Debug.Log($"[ArmouryWorldOfWarcraft] Frostmourne devoured a soul ({next}/150).");
            TryUpgradeAll();
        }

        public void HandleItemsAdded(ItemsCollection collection, ItemEntity item, int count)
        {
            if (collection != null) m_KnownCollections.Add(collection);
            if (!m_ChangingItems) TryUpgradeAll();
        }
        public void HandleItemsRemoved(ItemsCollection collection, ItemEntity item, int count)
        { if (collection != null) m_KnownCollections.Add(collection); }
        public void HandlePartyCombatStateChanged(bool inCombat)
        { if (!inCombat && m_DeferredByCombat) TryUpgradeAll(); }

        private static int GetSoulCount(BaseUnitEntity unit)
        {
            BlueprintBuff blueprint = ResourcesLibrary.TryGetBlueprint(SoulBuffGuid) as BlueprintBuff;
            if (unit == null || blueprint == null) return 0;
            Buff buff = unit.Buffs?.GetBuff(blueprint);
            return buff?.Rank ?? 0;
        }

        private static void SetSoulCount(BaseUnitEntity bearer, int count)
        {
            BlueprintBuff blueprint = ResourcesLibrary.TryGetBlueprint(SoulBuffGuid) as BlueprintBuff;
            if (bearer == null || blueprint == null)
            {
                Debug.LogError("[ArmouryWorldOfWarcraft] Souls Devoured buff blueprint was not found.");
                return;
            }
            Buff buff = bearer.Buffs.GetBuff(blueprint) ?? bearer.Buffs.Add(blueprint);
            int target = Math.Max(1, Math.Min(150, count));
            if (buff != null && buff.Rank < target) buff.AddRank(target - buff.Rank);
        }

        private int GetPartySoulCount()
        {
            Player player = Game.Instance?.Player;
            return player == null ? 0 : EnumeratePartyUnits(player).Select(GetSoulCount).DefaultIfEmpty(0).Max();
        }

        private void RememberPlayerCollections()
        {
            Player player = Game.Instance?.Player;
            if (player == null) return;
            if (player.Inventory != null) m_KnownCollections.Add(player.Inventory);
            if (player.SharedStash != null) m_KnownCollections.Add(player.SharedStash);
        }

        private void TryUpgradeAll()
        {
            Player player = Game.Instance?.Player;
            if (player == null || m_ChangingItems) return;
            if (IsCombatActive(player)) { m_DeferredByCombat = true; return; }
            m_DeferredByCombat = false;
            int souls = GetPartySoulCount();
            int targetTier = TierForSouls(souls);
            m_ChangingItems = true;
            try
            {
                foreach (ItemsCollection collection in m_KnownCollections.Where(value => value != null).ToArray())
                foreach (ItemEntity item in collection.Items.ToArray()) TryUpgradeItem(item, targetTier, souls);
                foreach (BaseUnitEntity unit in EnumeratePartyUnits(player))
                {
                    if (unit.Inventory?.Collection != null) m_KnownCollections.Add(unit.Inventory.Collection);
                    PropertyInfo bodyProperty = unit.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(property => typeof(PartUnitBody).IsAssignableFrom(property.PropertyType));
                    if (bodyProperty?.GetValue(unit) is PartUnitBody body)
                    foreach (ItemEntity item in body.Items.ToArray()) TryUpgradeItem(item, targetTier, souls);
                }
            }
            finally { m_ChangingItems = false; }
        }

        private static void TryUpgradeItem(ItemEntity item, int targetTier, int souls)
        {
            if (item?.Blueprint == null) return;
            int currentTier = Array.IndexOf(WeaponGuids, item.Blueprint.AssetGuid.ToString());
            if (currentTier < 0 || currentTier >= targetTier) return;
            BlueprintItem target = ResourcesLibrary.TryGetBlueprint(WeaponGuids[targetTier]) as BlueprintItem;
            if (target == null) { Debug.LogError("[ArmouryWorldOfWarcraft] Frostmourne tier blueprint not found: " + WeaponGuids[targetTier]); return; }
            var slot = item.HoldingSlot;
            ItemsCollection collection = item.Collection ?? slot?.MaybeOwnerInventory?.Collection;
            if (collection == null) return;
            if (slot != null && !slot.RemoveItem(false, false)) return;
            if (item.Collection != null) item.Collection.Remove(item);
            ItemEntity replacement = collection.Add(target);
            if (slot != null) slot.InsertItem(replacement, false);
            Debug.Log($"[ArmouryWorldOfWarcraft] Frostmourne awakened from V{currentTier + 1} to V{targetTier + 1} at {souls} souls.");
        }

        private static IEnumerable<BaseUnitEntity> EnumeratePartyUnits(Player player)
        {
            var result = new HashSet<BaseUnitEntity>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (PropertyInfo property in player.GetType().GetProperties(flags)
                .Where(property => property.Name.IndexOf("Party", StringComparison.OrdinalIgnoreCase) >= 0 && property.GetIndexParameters().Length == 0))
            {
                object value; try { value = property.GetValue(player); } catch { continue; }
                AddUnits(value, result);
            }
            foreach (FieldInfo field in player.GetType().GetFields(flags).Where(field => field.Name.IndexOf("Party", StringComparison.OrdinalIgnoreCase) >= 0))
                AddUnits(field.GetValue(player), result);
            return result;
        }
        private static void AddUnits(object value, HashSet<BaseUnitEntity> result)
        {
            if (value is BaseUnitEntity unit) result.Add(unit);
            else if (value is IEnumerable enumerable) foreach (object item in enumerable) if (item is BaseUnitEntity partyUnit) result.Add(partyUnit);
        }
        private static bool IsCombatActive(Player player) => player != null && (player.IsInCombat || EnumeratePartyUnits(player).Any(unit => unit != null && unit.IsInCombat));
        private static int TierForSouls(int souls)
        {
            if (souls >= 150) return 5; if (souls >= 120) return 4; if (souls >= 90) return 3;
            if (souls >= 60) return 2; if (souls >= 30) return 1; return 0;
        }
    }
}

