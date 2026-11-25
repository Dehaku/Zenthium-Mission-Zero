using System.Collections.Generic;
using NUnit.Framework;
using Zenthium.AI;
using Zenthium.Entities;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// enemies.md, Becoming aware: when the 0.5 s wait ends, unaware allies within 10 m become aware too, each with
    /// its own wait and alert, so alerts chain.
    /// </summary>
    public class AlertRulesTests
    {
        private const float Radius = 10f;

        [TestCase(10f, AwarenessLevel.Unaware, true, TestName = "Wakes_UnawareAllyAtTheEdge_True")]
        [TestCase(10.5f, AwarenessLevel.Unaware, false, TestName = "Wakes_AllyOutsideTheRadius_False")]
        [TestCase(5f, AwarenessLevel.Noticing, false, TestName = "Wakes_AllyAlreadyNoticing_False")]
        [TestCase(5f, AwarenessLevel.Aware, false, TestName = "Wakes_AllyAlreadyAware_False")]
        public void Wakes_SameTeam_DependsOnDistanceAndAwareness(float distance, AwarenessLevel level, bool expected)
        {
            Entity alerter = CombatTestSetup.CreateEntity(Team.Enemy);
            Entity ally = CombatTestSetup.CreateEntity(Team.Enemy);

            Assert.That(AlertRules.Wakes(alerter, ally, distance, Radius, level), Is.EqualTo(expected));
        }

        [TestCase(Team.Neutral)]
        [TestCase(Team.Player)]
        public void Wakes_OtherTeam_False(Team other)
        {
            Entity alerter = CombatTestSetup.CreateEntity(Team.Enemy);
            Entity candidate = CombatTestSetup.CreateEntity(other);

            Assert.That(AlertRules.Wakes(alerter, candidate, 1f, Radius, AwarenessLevel.Unaware), Is.False);
        }

        [Test]
        public void Alert_ChainsThroughAlliesEachWithinTheRadius()
        {
            // Goons in a line, 8 m apart, then one 14 m past the third: out of every radius.
            float[] x = { 0f, 8f, 16f, 30f };
            var goons = new List<Entity>();
            var awareness = new List<Awareness>();
            foreach (float _ in x)
            {
                goons.Add(CombatTestSetup.CreateEntity(Team.Enemy));
                awareness.Add(AwarenessTests.CreateAwareness());
            }

            awareness[0].Notice();
            Step(goons, awareness, x, 0.75f);
            AwarenessLevel thirdAt075 = awareness[2].Level;
            Step(goons, awareness, x, 0.25f);
            AwarenessLevel thirdAt1 = awareness[2].Level;
            Step(goons, awareness, x, 2f);

            Assert.That(thirdAt075, Is.EqualTo(AwarenessLevel.Unaware));
            Assert.That(thirdAt1, Is.EqualTo(AwarenessLevel.Noticing));
            Assert.That(awareness[2].Level, Is.EqualTo(AwarenessLevel.Aware));
            Assert.That(awareness[3].Level, Is.EqualTo(AwarenessLevel.Unaware));
        }

        /// <summary>Ticks everyone in 0.25 s steps; alerts sent in a step wake allies after everyone has ticked.</summary>
        private static void Step(List<Entity> goons, List<Awareness> awareness, float[] x, float seconds)
        {
            const float step = 0.25f;
            var alerters = new List<int>();
            for (float done = 0f; done < seconds; done += step)
            {
                alerters.Clear();
                for (int i = 0; i < awareness.Count; i++)
                {
                    if (awareness[i].Tick(step, 0f) == AwarenessChange.AlertSent)
                    {
                        alerters.Add(i);
                    }
                }

                foreach (int alerter in alerters)
                {
                    for (int j = 0; j < goons.Count; j++)
                    {
                        float distance = System.Math.Abs(x[alerter] - x[j]);
                        if (AlertRules.Wakes(goons[alerter], goons[j], distance, Radius, awareness[j].Level))
                        {
                            awareness[j].Notice();
                        }
                    }
                }
            }
        }
    }
}
