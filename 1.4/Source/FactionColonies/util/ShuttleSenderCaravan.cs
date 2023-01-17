using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies.util
{
    class ShuttleSenderCaravan : ShuttleSender
    {
        private readonly Caravan caravan;

        public ShuttleSenderCaravan(int Tile, Caravan caravan, WorldSettlementFC settlementFC) : base(Tile, settlementFC)
        {
			this.caravan = caravan;
        }

        public override bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            int tile = caravan.Tile;
            return CompLaunchable.ChoseWorldTarget(target, tile, Gen.YieldSingle(caravan), ShuttleRange, Launch, null);
        }

        public void Launch(int destinationTile, TransportPodsArrivalAction arrivalAction)
        {
            ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod);
            activeDropPod.Contents = new ActiveDropPodInfo();
            activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer(caravan.GetDirectlyHeldThings(), true, true);

            TravelingTransportPods travelingTransportPods = (TravelingTransportPods)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.TravelingShuttle);
            travelingTransportPods.Tile = Tile;
            travelingTransportPods.SetFaction(Faction.OfPlayer);
            travelingTransportPods.destinationTile = destinationTile;
            travelingTransportPods.arrivalAction = arrivalAction;
            travelingTransportPods.AddPod(activeDropPod.Contents, false);
            Find.WorldObjects.Add(travelingTransportPods);

            caravan.Destroy();
            settlementFC.shuttleUsesRemaining -= cost;
        }
	}
}
