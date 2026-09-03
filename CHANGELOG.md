# Changelog

All notable changes to **Armoury: Frostmourne** are documented here.

## 1.1.2 — One-handed Frostmourne and soul persistence

### Added

- Full one-handed Frostmourne progression from V1 to V6, available alongside the two-handed V1 in the Armoury chest.
- Both forms share collected souls and awaken at 150, 300, 450, 600 and 750 souls. Upgrades preserve the weapon's one-handed or two-handed form.
- One-handed base damage is 75% of the two-handed equivalent, rounded to whole numbers. The same abilities unlock at the same stages, with damage adapted to the one-handed weapon, including Harvest Soul.
- Finalized one-handed model scale, hand alignment and holster position.

### Fixed

- Collected souls now survive character knockdown, unconsciousness and death, preventing lost progression and subsequent weapon downgrades.
- One-handed Frostmourne participates in soul collection, the equipped soul display and automatic upgrades.
- Previous one-handed test weapons become regular V1 weapons and retain their existing blueprint ID for save compatibility.
- Corrected outdated soul thresholds in weapon descriptions.

### Installation

Download `Armoury_Frostmourne_1.1.2.zip` and install it with ModFinder. Fully close the game before updating and restart afterwards.

If you used an earlier test package or merged multiple versions manually, replace the old mod installation rather than copying files over it. Old DLLs and duplicate blueprints can prevent upgrades from working correctly. Keep your saves; collected souls remain compatible.

### Validation

Build and packaged blueprint/runtime checks passed. One-handed positioning and upgrades were tested in game by the author.

## 1.0.1 — Soul progression and UI fixes

### Changed

- Rebalanced Frostmourne's awakening thresholds to 150, 300, 450, 600 and 750 souls.
- Souls Devoured now continues counting after the final awakening, up to 999 souls.
- Souls Devoured is no longer marked as an important buff, reducing duplicate UI presentation.
- Changed the soul recovery panel shortcut from F8 to Ctrl + F8 to avoid conflicting with quickload.
- The persistent soul counter is now hidden while Frostmourne is unequipped and restored when the weapon is equipped again.

### Fixed

- Existing soul stacks remain compatible with the update through the unchanged persistent buff blueprint ID.
- Existing Frostmourne weapons now synchronize both upward and downward to the tier matching the retained soul count.
- Updating from 1.0.0 no longer requires removing Frostmourne or obtaining a replacement through ToyBox.
## 1.0.0 — Frostmourne V1

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
