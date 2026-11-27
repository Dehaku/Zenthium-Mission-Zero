using UnityEngine;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.World;

namespace Zenthium.AI
{
    /// <summary>
    /// Charming (modules.md) on an enemy goon: won over, it joins the charmer's side (an ally, for the player) and
    /// hands the charmer its Valuables. Its <see cref="AiBrain"/> reacts to the team change: it forgets the fight,
    /// leaves its squad and follows the player.
    /// </summary>
    [RequireComponent(typeof(EntityBehaviour), typeof(EntityModules))]
    public sealed class CharmConversion : MonoBehaviour, ICharmable
    {
        private EntityBehaviour self;
        private EntityModules modules;

        public bool TryGetCharmTarget(out CharmTarget target)
        {
            target = CharmTarget.Goon;
            Entity entity = self.Entity;
            return entity != null && entity.Team == Team.Enemy && entity.RankTier == RankTier.Goon
                && !entity.IsIncapacitated;
        }

        public void Charm(Entity charmer)
        {
            Entity entity = self.Entity;
            if (entity == null || charmer == null)
            {
                return;
            }

            entity.ChangeTeam(charmer.Team == Team.Player ? Team.Ally : charmer.Team);
            if (TryGetComponent(out ValuableCarrier carrier) && modules.World != null
                && modules.World.TryGetBehaviour(charmer, out EntityBehaviour charmerBody)
                && charmerBody.TryGetComponent(out ValuableCarrier receiver))
            {
                carrier.GiveAllTo(receiver);
            }
        }

        private void Awake()
        {
            self = GetComponent<EntityBehaviour>();
            modules = GetComponent<EntityModules>();
        }
    }
}
