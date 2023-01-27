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
                if (faction.powerOutput == null || faction.powerOutput.DestroyedOrNull() || faction.powerOutput == this.parent)
                {
                    faction.powerOutput = this.parent;
                    return Find.World.GetComponent<FactionFC>().powerPool;
                } else
                {
                    return 0f;
                }
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
