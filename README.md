# Armoury: Frostmourne

**Armoury: Frostmourne** adds the legendary runeblade Frostmourne to *Warhammer 40,000: Rogue Trader* as a fully modelled, progression-based player weapon.

This is the first standalone release developed in the **Armoury: World of Warcraft** repository. Future weapons are planned as separate mods and releases. Once the collection is complete, an optional combined **Armoury: World of Warcraft** package is planned.

## Features

- Custom Frostmourne model, PBR materials, inventory icon, weapon alignment and holster presentation.
- Six increasingly powerful variants with unique titles and statistics.
- Soul-based progression instead of character-level progression.
- A visible **Souls Devoured** buff tracks advancement.
- Three unlockable signature abilities with custom icons and effects.
- A persistent loot chest in the Warrant Chamber.
- No external mod dependency.

## Acquiring Frostmourne

Frostmourne can be acquired during the Prologue in the chamber containing the **Warrant of Trade**. Look for the dedicated dark runeblade chest positioned near the ceremonial floor area, slightly south of the *Armoury: Adeptus Custodes* chest when that mod is installed.

The chest contains **Frostmourne V1** in both two-handed and one-handed forms. Each form awakens automatically through V6 while retaining its weapon type.

For testing or existing campaigns, Frostmourne can also be added through ToyBox by searching for **Frostmourne**. The V1 blueprint ID is `f4a9c1e2837b4d5e8a6f9012bc34de56`.

## Soul progression

An enemy becomes marked after taking damage from Frostmourne. If that enemy dies afterwards, Frostmourne devours one soul. Enemies never touched by Frostmourne grant no soul.

The **Souls Devoured** buff displays the current total while Frostmourne is equipped. Unequipping the weapon hides this display without losing the persistent soul count. Outside combat, the weapon automatically awakens at the following thresholds:

| Variant | Souls | Title | Damage | Armour penetration | New ability |
| --- | ---: | --- | ---: | ---: | --- |
| V1 | 0 | Frostmourne | 16–23 | 30% | Runeblade Strike, Frozen Cleave |
| V2 | 150 | Frostmourne, Taker of Souls | 18–26 | 35% | Soulrend |
| V3 | 300 | Frostmourne, the Hungering Blade | 25–38 | 40% | — |
| V4 | 450 | Frostmourne, Bane of the Living | 35–46 | 45% | Chains of Ice |
| V5 | 600 | Frostmourne, Herald of Endless Winter | 38–56 | 50% | — |
| V6 | 750 | Frostmourne, End of All Things | 39–68 | 60% | Harvest Soul |

Weapon replacement is deferred until combat ends, preventing equipment changes during an encounter.
After the final awakening at 750 souls, the counter continues up to 999.

Existing soul stacks persist across mod updates. Outside combat, an existing Frostmourne automatically synchronizes upward or downward to the variant matching that retained total. If manual recovery is ever necessary, press **Ctrl + F8** outside combat to open the soul recovery panel and advance the counter to the appropriate awakening threshold.

### One-handed Frostmourne

Both forms share the same soul counter, awakening thresholds, armour penetration and ability unlocks. One-handed base damage is 75% of the corresponding two-handed damage, rounded to the nearest integer (halves rounded up).

| Variant | One-handed damage |
| --- | ---: |
| V1 | 12–17 |
| V2 | 14–20 |
| V3 | 19–29 |
| V4 | 26–35 |
| V5 | 29–42 |
| V6 | 29–51 |

Runeblade Strike, Frozen Cleave, Soulrend and Chains of Ice use the equipped weapon's damage. The one-handed Harvest Soul uses 58–102 base damage (twice one-handed V6) and retains 100% armour penetration. AP costs and control effects are unchanged. Existing one-handed test swords become the regular V1 and synchronize to the shared soul total outside combat.

## Abilities

- **Runeblade Strike** — a focused 1 AP Frostmourne attack.
- **Frozen Cleave** — a 2 AP sweeping melee attack.
- **Soulrend** — unlocked at V2; swings Frostmourne to tear at a target up to eight cells away and erupts with a psychic Purge Soul effect on a successful hit.
- **Chains of Ice** — unlocked at V4; encases and immobilizes a target in supernatural ice.
- **Harvest Soul** — unlocked at V6; deals 200% weapon damage with 100% armour penetration.

## Installation

Download `Armoury_Frostmourne_1.1.2.zip` from the latest GitHub release, add the ZIP to ModFinder for Rogue Trader, and enable **Armoury: Frostmourne**. Completely restart the game after installing or updating the mod. When updating from a test package or a mixed installation, replace the old mod folder instead of merging files to avoid retaining obsolete DLLs or duplicate blueprints. Keep your saved games.

## Development

The repository contains the mod-owned source assets, generated blueprints, editor generators and runtime code. Local Unity caches, extracted game data and build output are intentionally excluded.

## Disclaimer

This is an unofficial, non-commercial fan project. It is not affiliated with or endorsed by Blizzard Entertainment, Owlcat Games or Games Workshop. All referenced names and properties belong to their respective owners.
