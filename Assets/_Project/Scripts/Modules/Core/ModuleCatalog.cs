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
        /// <param name="problem">Why there's no behavior: none written yet, or data the behavior needs is missing.</param>
        public static bool TryCreate(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            problem = null;
            switch (data.Id)
            {
                case DataIds.Modules.Kick:
                case DataIds.Modules.SpeedPunch:
                case DataIds.Modules.Uppercut:
                    return TryCreateMelee(data, out behaviour, out problem);
                case DataIds.Modules.EnergyBlaster:
                    return TryCreateProjectile(data, out behaviour, out problem);
                case DataIds.Modules.Megaphone:
                    return TryCreateAreaStatus(data, StatusType.Stun, out behaviour, out problem);
                case DataIds.Modules.Dash:
                    if (!Require(data.RangeTier.IsSet, "a range tier", out problem))
                    {
                        return false;
                    }

                    behaviour = new DashModule(data);
                    return true;
                case DataIds.Modules.TimeSlow:
                case DataIds.Modules.TimeStop:
                    if (!Require(TimePowerModule.TryGetDuration(data, out float realSeconds),
                            TimePowerModule.DurationKey, out problem))
                    {
                        return false;
                    }

                    behaviour = new TimePowerModule(data, realSeconds);
                    return true;
                case DataIds.Modules.FirstAidSpray:
                    bool hasHeal = data.Values.TryGetNumber(FirstAidSprayModule.HealKey, out float heal);
                    if (!Require(data.RangeTier.IsSet && hasHeal, "a range tier and " + FirstAidSprayModule.HealKey,
                            out problem))
                    {
                        return false;
                    }

                    behaviour = new FirstAidSprayModule(data, heal);
                    return true;
                case DataIds.Modules.EvacBalloon:
                    if (!Require(data.RangeTier.IsSet, "a range tier", out problem))
                    {
                        return false;
                    }

                    behaviour = new EvacBalloonModule(data);
                    return true;
                case DataIds.Modules.PaddedSuit:
                    if (!Require(data.Values.TryGetNumber(ArmorPassive.ArmorKey, out float armor),
                            ArmorPassive.ArmorKey, out problem))
                    {
                        return false;
                    }

                    behaviour = new ArmorPassive(data, armor);
                    return true;
                case DataIds.Modules.WeatherTheStorm:
                    bool hasCost = data.TryGetResourceCost(out ResourceType resource, out float cost);
                    bool hasReduction = data.Values.TryGetNumber(
                        WeatherTheStormPassive.DamageReductionKey, out float reduction);
                    if (!Require(hasCost && hasReduction,
                            "a resource cost and " + WeatherTheStormPassive.DamageReductionKey, out problem))
                    {
                        return false;
                    }

                    behaviour = new WeatherTheStormPassive(data, resource, cost, reduction);
                    return true;
                case DataIds.Modules.Charming:
                    bool hasConvert = data.Values.TryGetNumber(CharmingPassive.GoonConvertKey, out float convert);
                    if (!Require(data.RangeTier.IsSet && hasConvert,
                            "a range tier and " + CharmingPassive.GoonConvertKey, out problem))
                    {
                        return false;
                    }

                    behaviour = new CharmingPassive(data, convert);
                    return true;
                case DataIds.Modules.Sprinter:
                case DataIds.Modules.ExpressDelivery:
                case DataIds.Modules.Immovable:
                    behaviour = new DataOnlyModule(data);
                    return true;
                default:
                    problem = "no behavior yet";
                    return false;
            }
        }

        private static bool TryCreateMelee(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!TryCreateDamage(data, out HitEffects effects, out problem)
                || !Require(data.RangeTier.IsSet, "a range tier", out problem))
            {
                return false;
            }

            behaviour = new MeleeStrikeModule(data, effects);
            return true;
        }

        private static bool TryCreateProjectile(ModuleData data, out ModuleBehaviour behaviour, out string problem)
        {
            behaviour = null;
            if (!TryCreateDamage(data, out HitEffects effects, out problem)
                || !Require(data.RangeTier.IsSet, "a range tier", out problem))
            {
                return false;
            }

            behaviour = new ProjectileModule(data, effects);
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

            data.TryGetDamageType(out DamageType type);
            StatFormula.TryCreate(data, out StatFormula damage);
            behaviour = new AreaStrikeModule(data,
                new HitEffects(data, damage, type, status, data.DurationTier.Value));
            return true;
        }

        private static bool TryCreateDamage(ModuleData data, out HitEffects effects, out string problem)
        {
            effects = null;
            if (!Require(StatFormula.TryCreate(data, out StatFormula damage), "a stat and a damage tier", out problem)
                || !Require(data.TryGetDamageType(out DamageType type), "a damage_type", out problem))
            {
                return false;
            }

            effects = new HitEffects(data, damage, type);
            return true;
        }

        private static bool Require(bool present, string what, out string problem)
        {
            problem = present ? null : $"its data needs {what}";
            return present;
        }
    }
}
