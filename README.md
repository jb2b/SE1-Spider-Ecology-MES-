# Spider Ecology 7.0 MES Creatures

This is the MES / Planet Creature Spawner variant of Spider Ecology.

## Required mods

- Modular Encounters Systems
- Planet Creature Spawner

Planet Creature Spawner disables vanilla wolf/spider spawning and lets MES handle creature spawning. This version therefore uses MES creature SpawnGroups instead of patching planet `AnimalSpawnInfo`.

## What it does

- EarthLike: wolves only
- Alien: full hive, all spider colors, brown/black dominate
- Mars: green/normal/brown, black at night
- Pertam: brown/black predators
- Titan: green scouts, rare normal at night
- Moon: brown/black, very rare in the normal build
- Europa/Triton: no managed wildlife

The C# script only provides global spider color combat rules:

- Green: scout, weaker, takes more damage
- Normal: worker, vanilla-like
- Brown: brute, tougher and hits harder
- Black: stalker, elite/tougher and hits harder

## Testing

Use `/MES.GESAP` at your current position and look for `Creature / Bot Eligible Spawns` entries with `SEco-Creature-*` names.

For fast testing, MES creature timing may also need to be lowered in the save config:

- `/MES.Settings.Creatures.MinCreatureSpawnTime.15`
- `/MES.Settings.Creatures.MaxCreatureSpawnTime.30`

The TEST build raises SpawnGroup frequency/chance, but MES global creature timers still control how often creature spawns are attempted.

## Notes

Do not run this together with older Spider Ecology versions. Use only one Spider Ecology mod at a time.
