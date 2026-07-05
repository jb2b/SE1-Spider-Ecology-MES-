using System;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace SpiderEcology
{
    /// <summary>
    /// Spider Ecology 7.0 - MES Creatures.
    ///
    /// This version does not patch PlanetGeneratorDefinition.AnimalSpawnInfo and does not force EnableWolfs/EnableSpiders.
    /// Creature spawning is provided through MES / Planet Creature Spawner SpawnGroups.
    /// This script only keeps the proven global spider color combat rules.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public sealed class SpiderEcologySession : MySessionComponentBase
    {
        private const int MaxDamageAdjustmentLogLines = 30;
        private int _damageAdjustmentLogCount;
        private bool _damageHandlerRegistered;

        public override void BeforeStart()
        {
            RegisterDamageHandler("BeforeStart");
        }

        protected override void UnloadData()
        {
            _damageHandlerRegistered = false;
            _damageAdjustmentLogCount = 0;
        }

        private void RegisterDamageHandler(string phase)
        {
            if (_damageHandlerRegistered)
                return;

            try
            {
                if (MyAPIGateway.Session == null || MyAPIGateway.Session.DamageSystem == null)
                {
                    Log("DamageSystem not available during " + phase + ". Global spider damage modifiers not registered.");
                    return;
                }

                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(500, OnBeforeDamageApplied);
                _damageHandlerRegistered = true;
                Log("Registered global spider variant damage handler during " + phase + ". MES SpawnGroups provide creatures; no planet spawn patch is used.");
            }
            catch (Exception ex)
            {
                Log("Failed to register spider variant damage handler during " + phase + ": " + ex);
            }
        }

        private void OnBeforeDamageApplied(object target, ref MyDamageInformation info)
        {
            try
            {
                if (info.Amount <= 0f || target == null)
                    return;

                var targetCharacter = target as IMyCharacter;

                // Incoming damage: target is spider -> effective toughness.
                if (targetCharacter != null && !targetCharacter.IsPlayer && !targetCharacter.IsDead)
                {
                    SpiderVariant targetVariant;
                    if (TryGetSpiderVariant(targetCharacter, out targetVariant))
                    {
                        ApplyDamageMultiplier(ref info, targetVariant.IncomingDamageMultiplier, targetVariant.Name, "incoming");
                        return;
                    }
                }

                // Outgoing damage: attacker is spider hitting a character/player.
                if (targetCharacter == null)
                    return;

                var attackerCharacter = TryGetAttackerCharacter(info.AttackerId);
                if (attackerCharacter == null || attackerCharacter.IsDead)
                    return;

                SpiderVariant attackerVariant;
                if (!TryGetSpiderVariant(attackerCharacter, out attackerVariant))
                    return;

                ApplyDamageMultiplier(ref info, attackerVariant.OutgoingDamageMultiplier, attackerVariant.Name, "outgoing");
            }
            catch (Exception ex)
            {
                if (_damageAdjustmentLogCount < MaxDamageAdjustmentLogLines)
                {
                    _damageAdjustmentLogCount++;
                    Log("Damage handler error: " + ex);
                }
            }
        }

        private static IMyCharacter TryGetAttackerCharacter(long attackerId)
        {
            if (attackerId == 0 || MyAPIGateway.Entities == null)
                return null;

            try
            {
                return MyAPIGateway.Entities.GetEntityById(attackerId) as IMyCharacter;
            }
            catch
            {
                return null;
            }
        }

        private void ApplyDamageMultiplier(ref MyDamageInformation info, float multiplier, string variant, string direction)
        {
            if (Math.Abs(multiplier - 1f) < 0.001f)
                return;

            var before = info.Amount;
            info.Amount = Math.Max(0f, before * multiplier);

            if (_damageAdjustmentLogCount < MaxDamageAdjustmentLogLines)
            {
                _damageAdjustmentLogCount++;
                Log("Adjusted " + direction + " damage for " + variant + ": " + before.ToString("0.##") + " -> " + info.Amount.ToString("0.##") + " (x" + multiplier.ToString("0.##") + ").");
            }
        }

        private static bool TryGetSpiderVariant(IMyCharacter character, out SpiderVariant variant)
        {
            variant = SpiderVariant.None;
            var definitionSubtype = SafeGetDefinitionSubtype(character);
            var friendlyName = SafeGetFriendlyName(character);

            if (EqualsIgnoreCase(definitionSubtype, "Space_spider_green") || Contains(friendlyName, "green"))
            {
                variant = SpiderVariant.GreenScout;
                return true;
            }

            if (EqualsIgnoreCase(definitionSubtype, "Space_spider_brown") || Contains(friendlyName, "brown"))
            {
                variant = SpiderVariant.BrownBrute;
                return true;
            }

            if (EqualsIgnoreCase(definitionSubtype, "Space_spider_black") || Contains(friendlyName, "black"))
            {
                variant = SpiderVariant.BlackStalker;
                return true;
            }

            if (EqualsIgnoreCase(definitionSubtype, "Space_spider") || Contains(friendlyName, "spider"))
            {
                variant = SpiderVariant.Worker;
                return true;
            }

            return false;
        }

        private static string SafeGetFriendlyName(IMyCharacter character)
        {
            try { return character.GetFriendlyName() ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static string SafeGetDefinitionSubtype(IMyCharacter character)
        {
            try { return character.Definition != null ? character.Definition.Id.SubtypeName : string.Empty; }
            catch { return string.Empty; }
        }

        private static bool Contains(string source, string value)
        {
            return !string.IsNullOrEmpty(source) && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool EqualsIgnoreCase(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static void Log(string message)
        {
            try { MyLog.Default.WriteLineAndConsole("[Spider Ecology] " + message); }
            catch { }
        }

        private struct SpiderVariant
        {
            public static readonly SpiderVariant None = new SpiderVariant(string.Empty, 1f, 1f);
            public static readonly SpiderVariant GreenScout = new SpiderVariant("Green spider / scout", 1.35f, 0.65f);
            public static readonly SpiderVariant Worker = new SpiderVariant("Normal spider / worker", 1.00f, 1.00f);
            public static readonly SpiderVariant BrownBrute = new SpiderVariant("Brown spider / brute", 0.60f, 1.40f);
            public static readonly SpiderVariant BlackStalker = new SpiderVariant("Black spider / stalker", 0.75f, 1.30f);

            public readonly string Name;
            public readonly float IncomingDamageMultiplier;
            public readonly float OutgoingDamageMultiplier;

            private SpiderVariant(string name, float incomingDamageMultiplier, float outgoingDamageMultiplier)
            {
                Name = name;
                IncomingDamageMultiplier = incomingDamageMultiplier;
                OutgoingDamageMultiplier = outgoingDamageMultiplier;
            }
        }
    }
}
