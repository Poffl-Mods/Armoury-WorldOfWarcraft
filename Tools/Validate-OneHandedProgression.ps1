$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$root = Join-Path $project 'Assets/Modifications/ArmouryWorldOfWarcraft'
$bp = Join-Path $root 'Blueprints'
$shared = Get-Content (Join-Path $root 'Scripts/FrostmourneOneHandedBlueprints.cs') -Raw
$ids = @([regex]::Matches($shared, '"([a-f0-9]{32})"') | ForEach-Object { $_.Groups[1].Value })
if ($ids.Count -ne 8 -or ($ids | Select-Object -Unique).Count -ne 8) { throw 'Invalid shared IDs' }
if ($ids[0] -ne '9d62b74c65f74c3a9c4100d4da41f033') { throw 'Legacy V1 ID changed' }
$localization = (Get-Content (Join-Path $root 'Localization/enGB.json') -Raw | ConvertFrom-Json).strings
$allIds = @()
foreach ($tier in 1..6) {
 $two = Get-Content (Join-Path $bp "Frostmourne_V${tier}_Item.jbp") -Raw | ConvertFrom-Json
 $oneFile = "Frostmourne_OneHanded_V${tier}_Item.jbp"
 $one = Get-Content (Join-Path $bp $oneFile) -Raw | ConvertFrom-Json
 $allIds += $two.AssetId; $allIds += $one.AssetId
 if ($one.AssetId -ne $ids[$tier-1]) { throw "V$tier ID mismatch" }
 foreach ($field in @('WarhammerDamage','WarhammerMaxDamage')) {
  $expected = [math]::Round($two.Data.$field * 0.75, [MidpointRounding]::AwayFromZero)
  if ($one.Data.$field -ne $expected -or $one.Data.m_Overrides -notcontains $field) { throw "V$tier $field mismatch" }
 }
 if ($one.Data.m_HoldingType -ne 'OneHanded' -or $one.Data.IsTwoHanded) { throw "V$tier holding type mismatch" }
 if ($one.Data.WarhammerPenetration -ne $two.Data.WarhammerPenetration) { throw "V$tier penetration mismatch" }
 foreach ($slotNum in 1..5) {
  $slot = "Ability$slotNum"
  $a = $one.Data.AbilityContainer.$slot; $b = $two.Data.AbilityContainer.$slot
  $expected = if ($tier -eq 6 -and $slotNum -eq 5) { '!bp_' + $ids[6] } else { $b.m_Ability }
  if ($a.m_Ability -ne $expected -or $a.AP -ne $b.AP -or $a.Type -ne $b.Type) { throw "V$tier $slot mismatch" }
 }
 $nameKey = $one.Data.m_DisplayName.m_Key
 if (-not $localization.$nameKey.Text -or $localization.$nameKey.Text -match 'Test') { throw "V$tier name missing" }
 $visual = $one.Data.m_VisualParameters
 foreach ($field in @('m_WeaponModel','m_WeaponBeltModelOverride')) {
  $metaName = if ($field -eq 'm_WeaponModel') { 'Frostmourne_OneHanded.prefab.meta' } else { 'Frostmourne_OneHanded_Holstered.prefab.meta' }
  $meta = Get-Content (Join-Path $root "Art/$metaName") -Raw
  if ($meta -notmatch [regex]::Escape($visual.$field.guid)) { throw "V$tier visual mismatch" }
 }
 "V${tier}: $($one.Data.WarhammerDamage)-$($one.Data.WarhammerMaxDamage); abilities and visuals OK"
}
if (($allIds | Select-Object -Unique).Count -ne 12) { throw 'Duplicate weapon IDs' }
$ability = Get-Content (Join-Path $bp 'Frostmourne_OneHanded_HarvestSoul_Ability.jbp') -Raw | ConvertFrom-Json
$override = @($ability.Data.Components | Where-Object { $_.'$type' -match 'WarhammerOverrideAbilityWeapon' })
if ($ability.AssetId -ne $ids[6] -or $override.Count -ne 1 -or $override[0].m_Weapon -ne ('!bp_' + $ids[7])) { throw 'Harvest override mismatch' }
$hidden = Get-Content (Join-Path $bp 'Frostmourne_HiddenOneHandedHarvest_Item.jbp') -Raw | ConvertFrom-Json
if ($hidden.AssetId -ne $ids[7] -or $hidden.Data.WarhammerDamage -ne 58 -or $hidden.Data.WarhammerMaxDamage -ne 102 -or $hidden.Data.WarhammerPenetration -ne 100) { throw 'Harvest damage mismatch' }
$buff = Get-Content (Join-Path $bp 'Frostmourne_SoulsDevoured_Buff.jbp') -Raw | ConvertFrom-Json
if ($buff.Data.m_Flags -notmatch 'StayOnDeath') { throw 'Soul persistence flag missing' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Join-Path $project 'Build/ArmouryWorldOfWarcraft.zip'))
try {
 $manifestEntry = $zip.Entries | Where-Object { $_.FullName -match '(^|/)OwlcatModificationManifest.json$' }
 $reader = [IO.StreamReader]::new($manifestEntry.Open()); try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
 if ($manifest.Version -ne '1.1.2') { throw 'ZIP version mismatch' }
 foreach ($file in Get-ChildItem $bp -Filter '*.jbp') {
  $entry = $zip.Entries | Where-Object { ($_.FullName -split '/')[-1] -eq $file.Name }
  if (-not $entry) { throw "ZIP missing $($file.Name)" }
  $reader = [IO.StreamReader]::new($entry.Open()); try { $packed = $reader.ReadToEnd() } finally { $reader.Dispose() }
  if ($packed -ne [IO.File]::ReadAllText($file.FullName)) { throw "ZIP blueprint differs: $($file.Name)" }
 }
} finally { $zip.Dispose() }
'PASS: 12 weapon tiers, legacy ID, damage scaling, unlocks, visuals, Harvest Soul, soul persistence and ZIP blueprints.'