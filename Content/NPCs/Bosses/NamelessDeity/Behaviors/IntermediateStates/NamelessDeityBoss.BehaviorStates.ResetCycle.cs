using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public void LoadStateTransitions_ResetCycle()
        {
            // Load the transition from RoarAnimation to the typical cycle.
            StateMachine.RegisterTransition(NamelessAIType.ResetCycle, null, false, () => true,
                () =>
                {
                    // Clear the state stack.
                    StateMachine.StateStack.Clear();

                    // Get the current attack cycle.
                    List<NamelessAIType> phaseCycle = Phase1Cycle.ToList();
                    if (CurrentPhase == 1)
                        phaseCycle = Phase2Cycle.ToList();
                    if (CurrentPhase == 2)
                        phaseCycle = Phase3Cycle.ToList();

                    // Insert glock shooting after the sword attack in GFB.
                    if (Main.zenithWorld)
                    {
                        for (int i = 0; i < phaseCycle.Count; i++)
                        {
                            if (phaseCycle[i] == NamelessAIType.SwordConstellation)
                            {
                                phaseCycle.Insert(i + 1, NamelessAIType.Glock);
                                i++;
                            }
                        }
                    }

                    // Supply the state stack with the attack cycle.
                    for (int i = phaseCycle.Count - 1; i >= 0; i--)
                        StateMachine.StateStack.Push(StateMachine.StateRegistry[phaseCycle[i]]);
                });
        }
    }
}
