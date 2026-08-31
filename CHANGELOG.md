# Changelog

All notable changes to **Armoury: Frostmourne** are documented here.

## 0.1.10 — Frostmourne V1

### Added

- Custom Frostmourne model, PBR materials and dedicated inventory icon.
- Completed drawn, holstered and inventory presentation.
- Six soul-powered Frostmourne variants with increasing damage and armour penetration.
- Persistent **Souls Devoured** buff and progression thresholds at 30, 60, 90, 120 and 150 souls.
- Soul credit for enemies previously damaged by Frostmourne and subsequently killed.
- Runeblade Strike and Frozen Cleave base attacks.
- Soulrend ranged sword attack with Purge Soul impact FX, unlocked at V2.
- Chains of Ice immobilization ability, unlocked at V4.
- Harvest Soul execution ability, unlocked at V6.
- Dedicated ability icons.
- Persistent Armoury: Frostmourne loot chest in the Warrant Chamber containing Frostmourne V1.
- F8 development panel for testing soul thresholds.

### Technical

- Weapon upgrades are deferred during combat and applied outside combat.
- Soulrend uses the equipped melee weapon's attack delivery over a custom eight-cell range.
- The chest uses its own persistent dynamic-map-object blueprint and can coexist with other Armoury chests.
