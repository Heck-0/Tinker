# Coverage report: compares the game's cards and relics with what Tinker supports.
#
# Needs the game decompiled once (about 30 s):
#   ilspycmd -p -o "$env:USERPROFILE\Documents\StS2ModSource\_sts2_full" -r "<game>\data_sts2_windows_x86_64" "<game>\data_sts2_windows_x86_64\sts2.dll"
# Re-run that after every game update, then run this script. Output: tools\coverage\cards.csv, relics.csv.
#
# Columns (cards.csv):  Vars = numeric values the card has; Upgradable = values Card Upgrader can bet on;
#   NotInTable = values VarDirections.cs has no entry for (not offered); Closed = values deliberately blocked.
# Columns (relics.csv): Vars = numeric values; Supported = values in Catalog.cs; Missing = the rest.
param(
    [string]$Game = "$env:USERPROFILE\Documents\StS2ModSource\_sts2_full",
    [string]$Src = (Join-Path $PSScriptRoot "..\src"),
    [string]$Out = (Join-Path $PSScriptRoot "coverage")
)
$ErrorActionPreference = 'Stop'
# Source files are found by name anywhere under src/, so moving them between folders doesn't break this.
function Src([string]$name) { $f = Get-ChildItem $Src -Recurse -Filter "$name.cs" | Select-Object -First 1; if (-not $f) { throw "source file $name.cs not found under $Src" }; $f.FullName }
New-Item -ItemType Directory -Force $Out | Out-Null

$defaultNames = @{ BlockVar='Block'; CardsVar='Cards'; DamageVar='Damage'; EnergyVar='Energy'; ExtraDamageVar='ExtraDamage'; ForgeVar='Forge'; GoldVar='Gold'; HealVar='Heal'; HitCountVar='HitCount'; HpLossVar='HpLoss'; MaxHpVar='MaxHp'; OstyDamageVar='OstyDamage'; RepeatVar='Repeat'; StarsVar='Stars'; SummonVar='Summon'; CalculationBaseVar='CalculationBase'; CalculationExtraVar='CalculationExtra'; CalculatedBlockVar='CalculatedBlock'; CalculatedDamageVar='CalculatedDamage'; IfUpgradedVar='IfUpgraded' }
$nonNumeric = 'BoolVar','StringVar','CalculatedVar','CalculatedBlockVar','CalculatedDamageVar','CalculatedHitCountVar','IfUpgradedVar'

function Get-Vars([string]$text) {
    $i = $text.IndexOf('CanonicalVars')
    if ($i -lt 0) { return @() }
    $rest = $text.Substring($i)
    $end = [regex]::Match($rest, '\r?\n\r?\n\t(?:public|protected|private|internal|\[)')
    $block = if ($end.Success) { $rest.Substring(0, $end.Index) } else { $rest }
    $vars = @()
    foreach ($m in [regex]::Matches($block, 'new (\w+Var)(?:<(\w+)>)?\(\s*("([^"]*)")?')) {
        $cls = $m.Groups[1].Value
        $name = if ($m.Groups[4].Success) { $m.Groups[4].Value } elseif ($m.Groups[2].Success) { $m.Groups[2].Value } else { $defaultNames[$cls] }
        if (-not $name) { $name = "?$cls" }
        $vars += [pscustomobject]@{ Name = $name; Class = $cls; Numeric = ($nonNumeric -notcontains $cls) }
    }
    $vars
}

# ---- Card Upgrader tables
$vd = Get-Content (Src VarDirections) -Raw
$sign = @{}
foreach ($m in [regex]::Matches($vd, '\[\("(\w+)", "(\w+)"\)\] = \((-?\d+), (\d+)\)')) { $sign["$($m.Groups[1].Value)|$($m.Groups[2].Value)"] = if ([int]$m.Groups[4].Value -ge 1) { [int]$m.Groups[3].Value } else { 0 } }
foreach ($m in [regex]::Matches($vd, '\[\("(\w+)", "(\w+)"\)\] = \((-?\d+), "')) { $sign["$($m.Groups[1].Value)|$($m.Groups[2].Value)"] = [int]$m.Groups[3].Value }
$cardSpecial = @{}
foreach ($f in 'FixedEffects','ExpandedEffects','BidirectionalEffects') {
    $t = Get-Content (Src $f) -Raw
    foreach ($m in [regex]::Matches($t, '"([A-Z]\w+)"')) { $cardSpecial[$m.Groups[1].Value] = $true }
}

$cards = foreach ($file in Get-ChildItem "$Game\MegaCrit.Sts2.Core.Models.Cards" -Filter *.cs) {
    $t = Get-Content $file.FullName -Raw
    $b = [regex]::Match($t, ':\s*base\((-?\d+|[^,]+),\s*CardType\.(\w+),\s*CardRarity\.(\w+)')
    if (-not $b.Success) { continue }
    $vars = Get-Vars $t
    $num = @($vars | Where-Object Numeric)
    $known = @($num | Where-Object { $sign.ContainsKey("$($file.BaseName)|$($_.Name)") })
    $open = @($num | Where-Object { $sign["$($file.BaseName)|$($_.Name)"] -ne $null -and $sign["$($file.BaseName)|$($_.Name)"] -ne 0 })
    $new = @($num | Where-Object { -not $sign.ContainsKey("$($file.BaseName)|$($_.Name)") })
    [pscustomobject]@{
        Card = $file.BaseName; Type = $b.Groups[2].Value; Rarity = $b.Groups[3].Value; Cost = $b.Groups[1].Value.Trim()
        Vars = ($num | ForEach-Object Name) -join ','
        Upgradable = ($open | ForEach-Object Name) -join ','
        Closed = (@($known | Where-Object { $sign["$($file.BaseName)|$($_.Name)"] -eq 0 }) | ForEach-Object Name) -join ','
        NotInTable = ($new | ForEach-Object Name) -join ','
        Special = [bool]$cardSpecial[$file.BaseName]
    }
}
$cards | Export-Csv "$Out\cards.csv" -NoTypeInformation -Encoding utf8

# ---- Relic Upgrader catalog
$cat = Get-Content (Src Catalog) -Raw
$relicSupport = @{}
foreach ($m in [regex]::Matches($cat, 'Group\("([^"]+)", "(\w+)"')) { foreach ($n in $m.Groups[1].Value.Split(' ', [StringSplitOptions]::RemoveEmptyEntries)) { $relicSupport["$n|$($m.Groups[2].Value)"] = $true; $relicSupport[$n] = $true } }
$relics = foreach ($file in Get-ChildItem "$Game\MegaCrit.Sts2.Core.Models.Relics" -Filter *.cs) {
    $t = Get-Content $file.FullName -Raw
    if ($t -notmatch ':\s*RelicModel') { continue }
    $r = [regex]::Match($t, 'Rarity => RelicRarity\.(\w+)')
    $num = @(Get-Vars $t | Where-Object Numeric)
    [pscustomobject]@{
        Relic = $file.BaseName; Rarity = if ($r.Success) { $r.Groups[1].Value } else { '?' }
        Vars = ($num | ForEach-Object Name) -join ','
        Supported = (@($num | Where-Object { $relicSupport["$($file.BaseName)|$($_.Name)"] }) | ForEach-Object Name) -join ','
        Missing = (@($num | Where-Object { -not $relicSupport["$($file.BaseName)|$($_.Name)"] }) | ForEach-Object Name) -join ','
    }
}
$relics | Export-Csv "$Out\relics.csv" -NoTypeInformation -Encoding utf8
$gameRelics = $relics | ForEach-Object Relic
$stale = $relicSupport.Keys | Where-Object { $_ -notmatch '\|' -and $gameRelics -notcontains $_ }
$stale | Set-Content "$Out\catalog_relics_not_in_game.txt"
"cards: $(@($cards).Count)  relics: $(@($relics).Count)  stale catalog relic names: $(@($stale).Count)"
