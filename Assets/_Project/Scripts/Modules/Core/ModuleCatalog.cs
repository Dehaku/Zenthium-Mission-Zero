using Zenthium.Data;
using Zenthium.Statuses;

namespace Zenthium.Modules
{
    /// <summary>
    /// Links each module's data id to its behavior, built from the shared parts (data-importer.md: "The data's
    /// Module ID is how the two are linked"). The numbers always come from the module's data.
    /// </summary>
    public static class ModuleCatalog
    {
        /// <param name="database">For modules that name other data, such as the entity a summon spawns.</param>
        /// <param name="problem">Why there's no behavior: none written yet, or data the behavior needs is missing.</param>
        public static bool TryCreate(ModuleData data, GameDatabase database, out ModuleBehaviour behaviour,
            out string problem)
        {
            behaviour = null;
            problem = null;
            switch (data.Id)
            {
                case DataIds.Modules.Kick:
                    return TryCreateKick(data, out behaviour, out problem);
                case DataIds.Modules.SpeedPunch:
                case DataIds.Modules.Uppercut:
                case DataIds.Modules.TabletSlap:
                    return TryCreateMelee(data, false, out behaviour, out problem);
                case DataIds.Modules.FinalNotice:
                    return TryCreateMarkingBeam(data, out behaviour, out problem);
                case DataIds.Modules.Repossess:
                    return TryCreateStatusBeam(data, StatusType.SlotDisable, out behaviour, out problem);
                case DataIds.Modules.TractorPull:
                    return TryCreatePull(data, PullDestination.LootDestination, false, out behaviour, out problem);
                case DataIds.Modules.TractorBeam:
                    return TryCreatePull(data, PullDestination.User, true, out behaviour, out problem);
                case DataIds.Modules.CallForBackup:
                    return TryCreateBackup(data, out behaviour, out problem);
                case DataIds.Modules.Audit:
                    return TryCreateNeedingRange(data, () => new AuditModule(data), out behaviour, out problem);
                case DataIds.Modules.Notarized:
                    return TryCreateNotarized(data, out behaviour, out problem);
                case DataIds.Modules.TowBeam:
                    return TryCreateNeedingRange(data, () => new TowBeamModule(data), out behaviour, out problem);
                case DataIds.Modules.InkLash:
                    // modules.md: Ink Lash scales with Rune's Will, the summoner's.
                    return TryCreateMelee(data, true, out behaviour, out problem);
                case DataIds.Modules.ShockwaveStomp:
                    return TryCreateAreaDamage(data, out behaviour, out problem);
                case DataIds.Modules.Megaphone:
                    return TryCreateAreaStatus(data, StatusType.Stun, out behaviour, out problem);
                case DataIds.Modules.EnergyBlaster:
                case DataIds.Modules.Cannons:
                case DataIds.Modules.FireWand:
                case DataIds.Modules.HeavyGun:
                    return TryCreateProjectile(data, out behaviour, out problem);
                case DataIds.Modules.RepulsorBlast:
                case DataIds.Modules.WaterCannon:
                    return TryCreateBeam(data, out behaviour, out problem);
                case DataIds.Modules.BreachingCharge:
                    return TryCreateCharge(data, out behaviour, out problem);
                case DataIds.Modules.GlyphTrap:
                    return TryCreateGlyph(data, out behaviour, out problem);
                case DataIds.Modules.SmokeBomb:
                    return TryCreateSmoke(data, out behaviour, out problem);
                case DataIds.Modules.EnergyShield:
                case DataIds.Modules.BarrierShield:
                    return TryCreateShield(data, out behaviour, out problem);
                case DataIds.Modules.Taunt:
                    return TryCreateTaunt(data, out behaviour, out problem);
                case DataIds.Modules.InkBeast:
                    return TryCreateSummon(data, database, DataIds.Entities.InkBeast, out behaviour, out problem);
                case DataIds.Modules.Dash:
                    return TryCreateNeedingRange(data, () => new DashModule(data), out behaviour, out problem);
                case DataIds.Modules.PowerThrow:
                    return TryCreateNeedingRange(data, () => new PowerThrowModule(data), out behaviour, out problem);
                case DataIds.Modules.EvacBalloon:
                    return TryCreateNeedingRange(data, () => new ExtractionModule(data, ExtractionMode.FloatAway),
                        out behaviour, out problem);
                case DataIds.Modules.StasisCuffs:
                    return TryCreateNeedingRange(data,
                        () => new ExtractionModule(data, ExtractionMode.CaptureInPlace), out behaviour, out problem);
                case DataIds.Modules.TimeSlow:
                case DataIds.Modules.TimeStop:
                    return TryCreateTimePower(data, out behaviour, out problem);
                case DataIds.Modules.FirstAidSpray:
                    return TryCreateFirstAid(data, out behaviour, out problem);
                case DataIds.Modules.PaddedSuit:
                    return TryCreateArmor(data, out behaviour, out problem);
                case DataIds.Modules.WeatherTheStorm:
                    return TryCreateWeatherTheStorm(data, out behaviour, out problem);
                case DataIds.Modules.Charming:
                    return TryCreateCharming(data, out behaviour, out problem);
                case DataIds.Modules.ReactivePlating:
                    return TryCreateReactivePlating(data, out behaviour, out problem);
                case DataIds.Modules.Overclocked:
                    return TryCreateOverclocked(data, out behaviour, out problem);
                case DataIds.Modules.DeathlyRage:
                    return TryCreateDeathlyRage(data, out behaviour, out problem);
                case DataIds.Modules.SecondWind:
                    return TryCreateHeal(data, HealTrigger.DefianceProc, HealOnTriggerPassive.DefianceHealKey,
                        out behaviour, out problem);
                case DataIds.Modules.FreshInk:
                    return TryCreateHeal(data, HealTrigger.DefeatedOpponent, HealOnTriggerPassive.DefeatHealKey,
                        out behaviour, out problem);
                case DataIds.Modules.Sprinter:
                case DataIds.Modules.ExpressDelivery:
                case DataIds.Modules.Immovable:
                case DataIds.Modules.HighCapacityBattery:
                case DataIds.Modules.LivingInk:
                case DataIds.Modules.ForceSpellTome:
                    behaviour = new DataOnlyModule(data);
                    return true;
                default:
                    problem = "no behavior yet";
                    return false;
            }
        }

        private static bool TryCreateMelee(ModuleData data, bool usesSummonerStats, out ModuleBehaviour behaviour,
            out string problem)
        {
            behaviour = null;
            if (!TryCreateDamage(data, usesSummonerStats, out HitEffects effects, out problem)
                || !Require(data.RangeTier.IsSet, "a range tier", out problem))
            {
                return false;
            }

            behaviour = new MeleeStrikeModule(data, effects);
            return true;
        }

        private static bool TryCreateKick(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!TryCreateDamage(data, false, out HitEffects effects, out problem)
                || !Require(data.RangeTier.IsSet && data.KnockbackTier.IsSet, "a range tier and a knockback tier",
                    out problem))
            {
                return false;
            }

            behaviour = new KickModule(data, effects);
            return true;
        }

        private static bool TryCreateAreaDamage(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!TryCreateDamage(data, false, out HitEffects effects, out problem)
                || !Require(data.RadiusTier.IsSet, "a radius tier", out problem))
            {
                return false;
            }

            behaviour = new AreaStrikeModule(data, effects);
            return true;
        }

        private static bool TryCreateAreaStatus(
            ModuleData data, StatusType status, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!Require(data.RadiusTier.IsSet && data.DurationTier.IsSet, "a radius tier and a duration tier",
                    out problem))
            {
                return false;
            }

            behaviour = new AreaStrikeModule(data, StatusOnly(data, status));
            return true;
        }

        private static bool TryCreateProjectile(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!TryCreateDamage(data, false, out HitEffects effects, out problem)
                || !Require(data.RangeTier.IsSet, "a range tier", out problem))
            {
                return false;
            }

            behaviour = new ProjectileModule(data, effects);
            return true;
        }

        private static bool TryCreateBeam(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!TryCreateDamage(data, false, out HitEffects effects, out problem)
                || !Require(data.RangeTier.IsSet, "a range tier", out problem))
            {
                return false;
            }

            behaviour = new BeamModule(data, effects);
            return true;
        }

        /// <summary>A beam that applies a status to the first entity it hits, for the duration tier (Repossess).</summary>
        private static bool TryCreateStatusBeam(ModuleData data, StatusType status, out ModuleBehaviour behaviour,
            out string problem)
        {
            behaviour = null;
            if (!Require(data.RangeTier.IsSet && data.DurationTier.IsSet, "a range tier and a duration tier",
                    out problem))
            {
                return false;
            }

            behaviour = new BeamModule(data, StatusOnly(data, status));
            return true;
        }

        /// <summary>A beam that Marks the first entity it hits (Final Notice), with Taunt's Marked keys.</summary>
        private static bool TryCreateMarkingBeam(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            DataValueList values = data.Values;
            bool complete = values.TryGetNumber(TauntModule.DurationKey, out float seconds)
                & values.TryGetNumber(TauntModule.MarkedDealtKey, out float dealt)
                & values.TryGetNumber(TauntModule.MarkedTakenKey, out float taken);
            if (!Require(complete && data.RangeTier.IsSet,
                    "a range tier, duration_s, marked_damage_dealt and marked_damage_taken", out problem))
            {
                return false;
            }

            data.TryGetDamageType(out DamageType type);
            var marked = new[]
            {
                new StatusModifier(StatusModifierTarget.DamageDealt, dealt),
                new StatusModifier(StatusModifierTarget.DamageTaken, taken),
            };
            behaviour = new BeamModule(data, new HitEffects(data, null, type, StatusType.Marked, seconds,
                statusModifiers: marked));
            return true;
        }

        private static bool TryCreatePull(ModuleData data, PullDestination destination, bool pullsEveryOpponent,
            out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!Require(data.RangeTier.IsSet && data.KnockbackTier.IsSet,
                    "a range tier and a knockback tier (the pull distance)", out problem))
            {
                return false;
            }

            data.TryGetDamageType(out DamageType type);
            behaviour = new PullModule(data, new HitEffects(data, null, type), destination, pullsEveryOpponent);
            return true;
        }

        private static bool TryCreateNotarized(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            bool hasReduction = data.Values.TryGetNumber(NotarizedPassive.DamageReductionKey, out float reduction);
            if (!Require(data.RadiusTier.IsSet && hasReduction,
                    "a radius tier and " + NotarizedPassive.DamageReductionKey, out problem))
            {
                return false;
            }

            behaviour = new NotarizedPassive(data, reduction);
            return true;
        }

        private static bool TryCreateBackup(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            bool complete = data.Values.TryGet(BackupRoster.GoonsKey, out DataValue goons)
                & data.Values.TryGet(BackupRoster.ChampionsKey, out DataValue champions);
            if (!Require(complete, $"{BackupRoster.GoonsKey} and {BackupRoster.ChampionsKey}", out problem))
            {
                return false;
            }

            behaviour = new CallForBackupModule(data, goons, champions);
            return true;
        }

        private static bool TryCreateCharge(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            bool hasFuse = data.Values.TryGetNumber(BreachingChargeModule.FuseKey, out float fuse);
            if (!TryCreateDamage(data, false, out HitEffects effects, out problem)
                || !Require(data.RangeTier.IsSet && data.RadiusTier.IsSet && hasFuse,
                    "a range tier, a radius tier and " + BreachingChargeModule.FuseKey, out problem))
            {
                return false;
            }

            behaviour = new BreachingChargeModule(data, effects, fuse);
            return true;
        }

        private static bool TryCreateGlyph(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!Require(data.RangeTier.IsSet && data.RadiusTier.IsSet && data.DurationTier.IsSet,
                    "a range tier, a radius tier and a duration tier", out problem))
            {
                return false;
            }

            behaviour = new GlyphTrapModule(data, StatusOnly(data, StatusType.Root));
            return true;
        }

        private static bool TryCreateSmoke(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!Require(data.RangeTier.IsSet && data.RadiusTier.IsSet && data.DurationTier.IsSet,
                    "a range tier, a radius tier and a duration tier", out problem))
            {
                return false;
            }

            behaviour = new SmokeBombModule(data);
            return true;
        }

        private static bool TryCreateShield(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            bool hasArc = data.Values.TryGetNumber(ShieldModule.ArcKey, out float arc);
            if (!Require(data.DurationTier.IsSet && hasArc, "a duration tier and " + ShieldModule.ArcKey,
                    out problem))
            {
                return false;
            }

            behaviour = new ShieldModule(data, data.DurationTier.Value, arc);
            return true;
        }

        private static bool TryCreateTaunt(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            DataValueList values = data.Values;
            bool complete = values.TryGetNumber(TauntModule.DurationKey, out float seconds)
                & values.TryGetNumber(TauntModule.MarkedDealtKey, out float dealt)
                & values.TryGetNumber(TauntModule.MarkedTakenKey, out float taken)
                & values.TryGetNumber(TauntModule.SelfReductionKey, out float selfReduction);
            if (!Require(complete, "duration_s, marked_damage_dealt, marked_damage_taken and self_damage_reduction",
                    out problem))
            {
                return false;
            }

            behaviour = new TauntModule(data, seconds, dealt, taken, selfReduction);
            return true;
        }

        private static bool TryCreateSummon(ModuleData data, GameDatabase database, string entityId,
            out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            bool hasLifetime = data.Values.TryGetNumber(SummonModule.LifetimeKey, out float lifetime);
            bool hasAttack = data.Values.TryGet(SummonModule.AttackKey, out DataValue attack)
                && attack.Kind == DataValueKind.Module && attack.Module != null;
            EntityData summoned = null;
            bool hasEntity = database != null && database.Entities.TryGet(entityId, out summoned);
            if (!Require(hasLifetime && hasAttack && hasEntity,
                    $"{SummonModule.LifetimeKey}, {SummonModule.AttackKey} and the entity '{entityId}'", out problem))
            {
                return false;
            }

            behaviour = new SummonModule(data, summoned, attack.Module, lifetime);
            return true;
        }

        private static bool TryCreateNeedingRange(ModuleData data, System.Func<ModuleBehaviour> create,
            out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!Require(data.RangeTier.IsSet, "a range tier", out problem))
            {
                return false;
            }

            behaviour = create();
            return true;
        }

        private static bool TryCreateTimePower(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!Require(TimePowerModule.TryGetDuration(data, out float realSeconds), TimePowerModule.DurationKey,
                    out problem))
            {
                return false;
            }

            behaviour = new TimePowerModule(data, realSeconds);
            return true;
        }

        private static bool TryCreateFirstAid(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            bool hasHeal = data.Values.TryGetNumber(FirstAidSprayModule.HealKey, out float heal);
            if (!Require(data.RangeTier.IsSet && hasHeal, "a range tier and " + FirstAidSprayModule.HealKey,
                    out problem))
            {
                return false;
            }

            behaviour = new FirstAidSprayModule(data, heal);
            return true;
        }

        private static bool TryCreateArmor(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!Require(data.Values.TryGetNumber(ArmorPassive.ArmorKey, out float armor), ArmorPassive.ArmorKey,
                    out problem))
            {
                return false;
            }

            behaviour = new ArmorPassive(data, armor);
            return true;
        }

        private static bool TryCreateWeatherTheStorm(ModuleData data, out ModuleBehaviour behaviour,
            out string problem)
        {
            behaviour = null;
            bool hasCost = data.TryGetResourceCost(out ResourceType resource, out float cost);
            bool hasReduction = data.Values.TryGetNumber(WeatherTheStormPassive.DamageReductionKey, out float reduction);
            if (!Require(hasCost && hasReduction, "a resource cost and " + WeatherTheStormPassive.DamageReductionKey,
                    out problem))
            {
                return false;
            }

            behaviour = new WeatherTheStormPassive(data, resource, cost, reduction);
            return true;
        }

        private static bool TryCreateCharming(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            bool hasConvert = data.Values.TryGetNumber(CharmingPassive.GoonConvertKey, out float convert);
            bool hasScare = data.Values.TryGetNumber(CharmingPassive.StubbornToScaredKey, out float scare);
            if (!Require(data.RangeTier.IsSet && hasConvert && hasScare,
                    "a range tier, " + CharmingPassive.GoonConvertKey + " and " + CharmingPassive.StubbornToScaredKey,
                    out problem))
            {
                return false;
            }

            behaviour = new CharmingPassive(data, convert, scare);
            return true;
        }

        private static bool TryCreateReactivePlating(ModuleData data, out ModuleBehaviour behaviour,
            out string problem)
        {
            behaviour = null;
            DataValueList values = data.Values;
            bool complete = values.TryGetNumber(ReactivePlatingPassive.ArmorPerHitKey, out float perHit)
                & values.TryGetNumber(ReactivePlatingPassive.ArmorMaxKey, out float max)
                & values.TryGetNumber(ReactivePlatingPassive.ResetKey, out float reset);
            if (!Require(complete, "armor_per_hit, armor_max and reset_s", out problem))
            {
                return false;
            }

            behaviour = new ReactivePlatingPassive(data, perHit, max, reset);
            return true;
        }

        private static bool TryCreateOverclocked(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            bool complete = data.Values.TryGetNumber(OverclockedPassive.DamageKey, out float damage)
                & data.Values.TryGetNumber(OverclockedPassive.ChargeAboveKey, out float threshold);
            if (!Require(complete, "damage and charge_above", out problem))
            {
                return false;
            }

            behaviour = new OverclockedPassive(data, damage, threshold);
            return true;
        }

        private static bool TryCreateDeathlyRage(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            DataValueList values = data.Values;
            bool complete = values.TryGetNumber(DeathlyRagePassive.StrengthKey, out float strength)
                & values.TryGetNumber(DeathlyRagePassive.ToughnessKey, out float toughness)
                & values.TryGetNumber(DeathlyRagePassive.DefianceBonusKey, out float bonus);
            if (!Require(complete, "strength_x, toughness_x and defiance_chance_bonus", out problem))
            {
                return false;
            }

            behaviour = new DeathlyRagePassive(data, strength, toughness, bonus);
            return true;
        }

        private static bool TryCreateHeal(ModuleData data, HealTrigger trigger, string key,
            out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!Require(data.Values.TryGetNumber(key, out float fraction), key, out problem))
            {
                return false;
            }

            behaviour = new HealOnTriggerPassive(data, trigger, fraction);
            return true;
        }

        private static bool TryCreateDamage(ModuleData data, bool usesSummonerStats, out HitEffects effects,
            out string problem)
        {
            effects = null;
            if (!Require(StatFormula.TryCreate(data, out StatFormula damage), "a stat and a damage tier", out problem)
                || !Require(data.TryGetDamageType(out DamageType type), "a damage_type", out problem))
            {
                return false;
            }

            effects = new HitEffects(data, damage, type, usesSummonerStats: usesSummonerStats);
            return true;
        }

        /// <summary>No damage; the status for the duration tier, resisted by the module's Resist tag.</summary>
        private static HitEffects StatusOnly(ModuleData data, StatusType status)
        {
            data.TryGetDamageType(out DamageType type);
            return new HitEffects(data, null, type, status, data.DurationTier.Value);
        }

        private static bool Require(bool present, string what, out string problem)
        {
            problem = present ? null : $"its data needs {what}";
            return present;
        }
    }
}
