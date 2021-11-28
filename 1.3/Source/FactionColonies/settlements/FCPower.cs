using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using HarmonyLib;
using Verse.AI;

namespace FactionColonies
{
    public class CompPowerEmpire : CompPowerPlant
    {
        

        protected override float DesiredPowerOutput
        {
            get
            {
                FactionFC faction = Find.World.GetComponent<FactionFC>();
                var finalOutput = 0f;
                if (faction.powerOutput == null || faction.powerOutput.DestroyedOrNull() || faction.powerOutput == this.parent)
                {
                    faction.powerOutput = this.parent;
                    var maxPowerOutput = Find.World.GetComponent<FactionFC>().powerPool;
                    // If the faction generates less power than this can process
                    if (base.DesiredPowerOutput > maxPowerOutput)
                    {
                        finalOutput = maxPowerOutput;
                    }
                    // If the faction generates more power than this can process
                    else
                    {
                        finalOutput = base.DesiredPowerOutput;
                    }
                }

                return finalOutput;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            base.CompGetGizmosExtra();
            if (this.parent.Faction == Faction.OfPlayer) {
                yield return new Command_Action
                {
                    action = delegate ()
                    {
                        Find.World.GetComponent<FactionFC>().powerOutput = this.parent;
                        Messages.Message("SetAsOutputSuccess".Translate(), MessageTypeDefOf.NeutralEvent);
                    },
                    defaultDesc = "SetAsEmpirePowerOutput".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/TryReconnect", true),
                    defaultLabel = "SetAsOutput".Translate()
                };
            }
        }


    }
}
