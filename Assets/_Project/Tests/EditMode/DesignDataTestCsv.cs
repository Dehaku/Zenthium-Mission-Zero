using System.Collections.Generic;
using Zenthium.Editor.DataImport;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// A small, valid set of design CSVs for importer tests. Tests add rows to it.
    /// </summary>
    internal static class DesignDataTestCsv
    {
        public const string Constants =
            "group,name,value,notes\n" +
            "formula,hp_per_toughness,10,\n" +
            "formula,leader_hp_multiplier,2,\n" +
            "formula,armor_per_toughness,0.5,\n" +
            "formula,normal_damage_base,10,\n" +
            "formula,normal_damage_per_stat,2,\n" +
            "formula,kick_cooldown,3,\n" +
            "formula,min_damage_fraction,0.05,\n" +
            "formula,dodge_per_finesse,0.005,\n" +
            "formula,dodge_cap,0.75,\n" +
            "formula,vision_range,100,\n" +
            "formula,awareness_instant_range,10,\n" +
            "formula,awareness_range,15,\n" +
            "formula,awareness_max_time,10,\n" +
            "formula,resource_seconds_per_point,1,\n" +
            "formula,enemy_default_damage_tier,Light,\n" +
            "formula,base_stat,10,\n" +
            "formula,base_move_speed,6,\n" +
            "formula,defiance_chance_per_will,0.001,\n" +
            "formula,defiance_roll_cost,1,\n" +
            "formula,defiance_invulnerable_player,5,\n" +
            "formula,defiance_invulnerable_other,1,\n" +
            "formula,resource_regen_per_second,1,\n" +
            "formula,slots_per_type,4,\n" +
            "formula,preset_slots_unlocked,1,\n" +
            "formula,cc_duration_multiplier,0.5,\n" +
            "formula,cc_pulse_on_s,0.5,\n" +
            "formula,cc_pulse_off_s,0.5,\n" +
            "formula,dot_tick_s,0.5,\n" +
            "mirror,innate_boost_goon,0,\n" +
            "mirror,innate_boost_champion,1,\n" +
            "mirror,innate_boost_leader,2,\n" +
            "mirror,stat_bonus_per_rank,0.2,\n" +
            "mirror,hp_damage_bonus_per_rank,0.1,\n" +
            "reference,goon_stat,10,\n" +
            "economy,income_basic,100,\n" +
            "economy,income_gem,20,\n" +
            "economy,income_metallic,2,\n" +
            "economy,stat_price_increase_per_point,0.1,\n" +
            "economy,income_mix.Rescue.basic,1.5,\n" +
            "tier,damage.Light,0.5,\n" +
            "tier,damage.Normal,1,\n" +
            "tier,damage.Heavy,2,\n" +
            "tier,duration.Brief,1,\n" +
            "tier,knockback.Normal,4,\n" +
            "tier,range.Melee,2,\n" +
            "tier,utility.None,1,\n" +
            WorldConstants;

        /// <summary>The world group, which tests can leave a row out of.</summary>
        public const string WorldConstants =
            "world,material_hp.Glass,10,\n" +
            "world,material_hp.Wood,100,\n" +
            "world,material_hp.Stone,300,\n" +
            "world,material_hp.Metal,600,\n" +
            "world,material_hp.Crystalline,1000,\n" +
            "world,impact_structure_damage_tier,Heavy,\n" +
            "world,impact_entity_damage_tier,Light,\n" +
            "world,building_impact_multiplier,10,\n" +
            "world,building_throw_multiplier,2,\n" +
            "world,demolition_multiplier,10,\n" +
            "world,vehicle_explosion_delay_s,1,\n" +
            "world,building_neighbor_radius_m,20,\n" +
            "world,fire_tick_s,5,\n" +
            "world,fire_break_interval_s,10,\n" +
            "world,fire_burn_damage,20,\n" +
            "world,fire_burn_s,4,\n" +
            "world,burn_out_s,60,\n" +
            "world,fire_spread_interval_s,15,\n" +
            "world,fire_spread_chance,0.25,\n" +
            "world,prop_burn_duration_multiplier,10,\n" +
            "world,carrying_treasure_slow_per_valuable,0.05,\n" +
            "world,carrying_treasure_slow_cap,0.5,\n" +
            "world,valuable_icon_growth_per_valuable,0.05,\n" +
            "world,valuable_icon_growth_cap,0.5,\n" +
            "world,valuable_pickup_s,5,\n" +
            "ai,alert_delay_s,0.5,\n" +
            "ai,alert_radius_m,10,\n" +
            "ai,search_s,10,\n" +
            "ai,awareness_drain_s,3,\n" +
            "ai,ranged_backoff_m,3,\n" +
            "ai,ally_follow_m,10,\n" +
            "ai,telegraph_s,0.5,\n" +
            "ai,patrol_speed_fraction,0.5,\n";

        private const string ModulesHeader =
            "module,category,stat,damage_tier,cooldown_s,resource,cost,utility,duration_tier,radius_tier,knockback_tier,range_tier,values,damage_type,tags,budget_ok,notes\n";

        private const int ModuleColumnCount = 17;

        public static readonly string KickRow =
            ModuleRow("Kick", "Base action", "Strength", "Normal", "3", "", "", "None", "", "", "Normal", "Melee");

        private static readonly string InkLashRow =
            ModuleRow("Ink Lash", "Active", "Will", "Light", "1.5", "", "", "None", "", "", "", "Melee");

        private const string EntitiesCsv =
            "faction,entity,rank_tier,min_rank,budget_cost,values,notes\n" +
            "Galactic Repo Agency,Grey,Goon,D,1,,\n";

        private const string HeroesHeader =
            "hero,strength,will,intelligence,finesse,toughness,starter,actives,passives,gear,notes\n";

        private const string RushHourRow = "Rush Hour,20,10,10,20,10,yes,,,,\n";

        private const string PropsCsv =
            "name,size,strength_needed,hp,thrown_x_kick,armor,damage_reduction,explosion_damage,explosion_radius_m,notes\n" +
            "\"Car, truck\",Large,250,100,3,20,0.5,500,15,Vehicle\n";

        private const string PricesCsv =
            "purchase,missions,currency,notes\n" +
            "Stat point,1,basic,\n";

        /// <summary>A modules.csv row in column order (module, category, stat, damage_tier, cooldown_s,
        /// resource, cost, utility, duration_tier, radius_tier, knockback_tier, range_tier, values, damage_type, tags...).
        /// Cells left off are blank.</summary>
        public static string ModuleRow(params string[] cells)
        {
            var padded = new List<string>(cells);
            while (padded.Count < ModuleColumnCount)
            {
                padded.Add(string.Empty);
            }

            return string.Join(",", padded) + "\n";
        }

        /// <summary>An Active module whose only set cells are Will, a 30 s cooldown and its values.</summary>
        public static string ModuleRowWithValues(string id, string values)
        {
            return ModuleRow(id, "Active", "Will", "", "30", "", "", "", "", "", "", "", values);
        }

        /// <param name="constants">Replaces the whole constants file, such as one with a row left out.</param>
        public static DesignDataParseResult Parse(
            string extraModuleRows = "", string extraHeroRows = "", string extraConstantRows = "",
            string constants = Constants)
        {
            return DesignDataParser.Parse(new DesignDataSource(
                constants + extraConstantRows,
                ModulesHeader + KickRow + InkLashRow + extraModuleRows,
                EntitiesCsv,
                HeroesHeader + RushHourRow + extraHeroRows,
                PropsCsv,
                PricesCsv));
        }
    }
}
