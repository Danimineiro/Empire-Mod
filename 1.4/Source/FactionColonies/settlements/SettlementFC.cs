using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using FactionColonies.util;

namespace FactionColonies
{
    public class SettlementFC : IExposable, ILoadReferenceable
    {
        /// <summary>
        /// Used by other mods to get to our world object through the list of SettlementFC. Rename at your own risk.
        /// </summary>
        public WorldSettlementFC worldSettlement;

        public string GetUniqueLoadID()
        {
            return "SettlementFC_" + loadID;
        }

        public bool IsBeingUpgraded => Find.World.GetComponent<FactionFC>().events.Any(evt => evt.def == FCEventDefOf.upgradeSettlement && evt.location == mapLocation);

        /// <summary>
        /// Required for saving/loading
        /// </summary>
        public SettlementFC()
        {
        }

        public SettlementFC(string name, int location)
        {
            this.name = name;
            mapLocation = location;
            planetName = Find.World.info.name;
            loadID = Find.World.GetComponent<FactionFC>().GetNextSettlementFCID();

            settlementLevel = 1;

            //Efficiency Multiplier
            productionEfficiency = 1.0;
            workers = 0;
            workersMax = settlementLevel * 3 + returnMaxWorkersFromPrisoners();
            workersUltraMax = workersMax + 5 + returnOverMaxWorkersFromPrisoners();


            // Log.Message(Find.WorldGrid.tiles[location].biome.ToString());   <= Returns biome
            //biome info
            biome = Find.WorldGrid.tiles[location].biome.ToString();

            hilliness = Find.WorldGrid.tiles[location].hilliness.ToString();

            //modded biomes
            biomeDef = DefDatabase<BiomeResourceDef>.GetNamed(biome, false) ?? BiomeResourceDefOf.defaultBiome;

            //Log.Message(hilliness);
            hillinessDef = DefDatabase<BiomeResourceDef>.GetNamed(hilliness);

            for (int i = 0; i < 8; i++)
            {
                buildings.Add(BuildingFCDefOf.Empty);
            }


            //Settlment resources
            food = new ResourceFC(0, ResourceType.Food, this);
            weapons = new ResourceFC(0, ResourceType.Weapons, this);
            apparel = new ResourceFC(0, ResourceType.Apparel, this);
            animals = new ResourceFC(0, ResourceType.Animals, this);
            logging = new ResourceFC(0, ResourceType.Logging, this);
            mining = new ResourceFC(0, ResourceType.Mining, this);
            power = new ResourceFC(0, ResourceType.Power, this);
            medicine = new ResourceFC(0, ResourceType.Medicine, this);
            research = new ResourceFC(0, ResourceType.Research, this);

            foreach (ResourceType titheType in ResourceUtils.resourceTypes)
            {
                PaymentUtil.resetThingFilter(this, titheType);
            }

            initBaseProduction();
            updateProduction();
        }

        public void addPrisoner(Pawn prisoner)
        {
            prisonerList.Add(new FCPrisoner(prisoner, this));
            //Log.Message(prisoners.Count().ToString());
        }

        public int NumberBuildings => 3 + (int) Math.Floor(settlementLevel / 2f);

        public void upgradeSettlement(int times = 1)
        {
            settlementLevel += times;
            if (settlementLevel > 10) settlementLevel = 10;
            if (settlementLevel < 0) settlementLevel = 0;
            updateStats();
        }

        public void delevelSettlement(int times = -1)
        {
            upgradeSettlement(times);
        }

        public void tickSpecialActions(int tick)
        {
            if (trait_Egalitarian_TaxBreak_Enabled &&
                tick >= trait_Egalitarian_TaxBreak_Tick + GenDate.TicksPerDay * 10)
                trait_Egalitarian_TaxBreak_Enabled = false;

            if (lastShuttleUsesTick < (tick - GenDate.TicksPerDay * 5))
            {
                int temp = 0;
                buildings.ForEach(building => temp += building.shuttleUses);
                worldSettlement.shuttleUsesRemaining = temp;
                lastShuttleUsesTick = tick;
            }
        }

        public void initBaseProduction()
        {
            foreach (ResourceType titheType in ResourceUtils.resourceTypes)
            {
                ResourceFC resource = getResource(titheType);
                resource.baseProduction = biomeDef.BaseProductionAdditive[(int) titheType]
                                          + hillinessDef.BaseProductionAdditive[(int) titheType];
                resource.baseProduction = biomeDef.BaseProductionMultiplicative[(int) titheType]
                                          + hillinessDef.BaseProductionMultiplicative[(int) titheType];
                resource.settlement = this;
            }
        }

        internal void GainUnrestWithReason(Message message, double amount)
        {
            Messages.Message(message);
            unrest += amount * TraitUtilsFC.cycleTraits("unrestGainedMultiplier", traits, Operation.Multiplication) * TraitUtilsFC.cycleTraits("unrestGainedMultiplier", Find.World.GetComponent<FactionFC>().traits, Operation.Multiplication);
        }

        internal void GainHappiness(double amount)
        {
            happiness -= 10 * TraitUtilsFC.cycleTraits("happinessLostMultiplier", traits, Operation.Multiplication) * TraitUtilsFC.cycleTraits("happinessLostMultiplier", Find.World.GetComponent<FactionFC>().traits, Operation.Multiplication);
        }

        public void updateProfitAndProduction() //updates both profit and production
        {
            updateProduction();
            updateProfit();
            updateStats();
        }

        public void updateStats()
        {
            FactionFC factionFc = Find.World.GetComponent<FactionFC>();
            int isolationistExtraWorkers = 0;

            if (factionFc.hasPolicy(FCPolicyDefOf.isolationist))
                isolationistExtraWorkers += 3;
            //Military Settlement Level
            settlementMilitaryLevel = settlementLevel - 1 + Convert.ToInt32(
                TraitUtilsFC.cycleTraits("militaryBaseLevel", traits, Operation.Addition) +
                TraitUtilsFC.cycleTraits("militaryBaseLevel", Find.World.GetComponent<FactionFC>().traits,
                    Operation.Addition));

            //Worker Stats
            workersMax = (settlementLevel * (3 + isolationistExtraWorkers)) +
                         (TraitUtilsFC.cycleTraits("workerBaseMax", traits, Operation.Addition) +
                          TraitUtilsFC.cycleTraits("workerBaseMax",
                              Find.World.GetComponent<FactionFC>().traits, Operation.Addition)) + returnMaxWorkersFromPrisoners();
            workersUltraMax = (workersMax + 5 +
                               (TraitUtilsFC.cycleTraits("workerBaseOverMax", traits, Operation.Addition) +
                                TraitUtilsFC.cycleTraits("workerBaseOverMax",
                                    Find.World.GetComponent<FactionFC>().traits, Operation.Addition)) +
                               returnOverMaxWorkersFromPrisoners());
        }

        public void updateProfit() //updates profit
        {
            totalUpkeep = getTotalUpkeep();
            updateWorkerCost();
            totalIncome = getTotalIncome();
            totalProfit = Convert.ToInt32(totalIncome - totalUpkeep);
        }


        public void updateHappiness()
        {
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();
            double happinessGainMultiplier =
                 TraitUtilsFC.cycleTraits("happinessGainedMultiplier", traits, Operation.Multiplication) *
                 TraitUtilsFC.cycleTraits("happinessGainedMultiplier",
                     Find.World.GetComponent<FactionFC>().traits, Operation.Multiplication);
            double happinessLostMultiplier =
                 TraitUtilsFC.cycleTraits("happinessLostMultiplier", traits, Operation.Multiplication) *
                 TraitUtilsFC.cycleTraits("happinessLostMultiplier",
                     Find.World.GetComponent<FactionFC>().traits, Operation.Multiplication);

            double policyIncrease = 0;
            if (factionfc.hasPolicy(FCPolicyDefOf.egalitarian) && trait_Egalitarian_TaxBreak_Enabled)
                policyIncrease = 2;


            happiness += happinessGainMultiplier * (policyIncrease + FactionColonies.happinessBaseGain +
                                                    TraitUtilsFC.cycleTraits("happinessGainedBase",
                                                        traits, Operation.Addition) +
                                                    TraitUtilsFC.cycleTraits("happinessGainedBase",
                                                        Find.World.GetComponent<FactionFC>().traits, Operation.Addition)
                ); //Go through traits and add happiness where needed
            happiness -= happinessLostMultiplier * (FactionColonies.happinessBaseLost +
                                                    TraitUtilsFC.cycleTraits("happinessLostBase", traits,
                                                        Operation.Addition) + TraitUtilsFC.cycleTraits("happinessLostBase",
                                                        Find.World.GetComponent<FactionFC>().traits, Operation.Addition)
                ); //Go through traits and remove happiness where needed

            happiness = Math.Round(happiness, 1);

            if (happiness <= 0)
            {
                happiness = 1;
            }

            if (happiness > 100)
            {
                happiness = 100;
            }
        }

        public void updateLoyalty()
        {
            double loyaltyGainMultiplier =
                (TraitUtilsFC.cycleTraits("loyaltyGainedMultiplier", traits, Operation.Multiplication) * TraitUtilsFC.cycleTraits("loyaltyGainedMultiplier", Find.World.GetComponent<FactionFC>().traits, Operation.Multiplication));
            double loyaltyLostMultiplier =
                (TraitUtilsFC.cycleTraits("loyaltyLostMultiplier", traits, Operation.Multiplication) * TraitUtilsFC.cycleTraits("loyaltyLostMultiplier", Find.World.GetComponent<FactionFC>().traits, Operation.Multiplication));

            loyalty += loyaltyGainMultiplier * (FactionColonies.loyaltyBaseGain + TraitUtilsFC.cycleTraits("loyaltyGainedBase", traits, Operation.Addition) + TraitUtilsFC.cycleTraits("loyaltyGainedBase", Find.World.GetComponent<FactionFC>().traits, Operation.Addition)
                ); //Go through traits and add loyalty where needed
            loyalty -= loyaltyLostMultiplier * (FactionColonies.loyaltyBaseLost + TraitUtilsFC.cycleTraits("loyaltyLostBase", traits, Operation.Addition) + TraitUtilsFC.cycleTraits("loyaltyLostBase", Find.World.GetComponent<FactionFC>().traits, Operation.Addition)
                ); //Go through traits and remove loyalty where needed

            loyalty = Math.Round(loyalty, 1);

            if (loyalty <= 0)
            {
                loyalty = 1;
            }

            if (loyalty > 100)
            {
                loyalty = 100;
            }
        }

        public void updateProsperity()
        {
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();
            double policyIncrease = 0;
            if (factionfc.hasPolicy(FCPolicyDefOf.egalitarian) && trait_Egalitarian_TaxBreak_Enabled)
                policyIncrease = 2;

            prosperity += (policyIncrease + FactionColonies.prosperityBaseRecovery + TraitUtilsFC.cycleTraits("prosperityBaseRecovery", traits, Operation.Addition) + TraitUtilsFC.cycleTraits("prosperityBaseRecovery", Find.World.GetComponent<FactionFC>().traits, Operation.Addition)
                ); //Go through traits and add prosperity where needed

            prosperity = Math.Round(prosperity, 1);

            if (prosperity <= 0)
            {
                prosperity = 1;
            }

            if (prosperity > 100)
            {
                prosperity = 100;
            }
        }

        public void updateUnrest()
        {
            double unrestGainMultiplier =
                (TraitUtilsFC.cycleTraits("unrestGainedMultiplier", traits, Operation.Multiplication) * TraitUtilsFC.cycleTraits("unrestGainedMultiplier", Find.World.GetComponent<FactionFC>().traits, Operation.Multiplication));
            double unrestLostMultiplier =
                (TraitUtilsFC.cycleTraits("unrestLostMultiplier", traits, Operation.Multiplication) * TraitUtilsFC.cycleTraits("unrestLostMultiplier", Find.World.GetComponent<FactionFC>().traits, Operation.Multiplication));

            unrest += unrestGainMultiplier * (FactionColonies.unrestBaseGain + TraitUtilsFC.cycleTraits("unrestGainedBase", traits, Operation.Addition) + TraitUtilsFC.cycleTraits("unrestGainedBase", Find.World.GetComponent<FactionFC>().traits, Operation.Addition)
                ); //Go through traits and add unrest where needed
            unrest -= unrestLostMultiplier * (FactionColonies.unrestBaseLost + TraitUtilsFC.cycleTraits("unrestLostBase", traits, Operation.Addition) + TraitUtilsFC.cycleTraits("unrestLostBase", Find.World.GetComponent<FactionFC>().traits, Operation.Addition)
                ); //Go through traits and remove unrest where needed

            unrest = Math.Round(unrest, 1);

            if (unrest < 0)
            {
                unrest = 0;
            }

            if (unrest > 100)
            {
                unrest = 100;
            }
        }


        public void updateProduction() //updates production of settlemetns
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            double egalitarianTaxBoost = 0;
            if (faction.hasPolicy(FCPolicyDefOf.egalitarian))
            {
                egalitarianTaxBoost = Math.Floor(happiness / 10);
                if (trait_Egalitarian_TaxBreak_Enabled)
                {
                    egalitarianTaxBoost -= 30;
                }
            }

            double isolationistTaxBoost = 0;
            if (faction.hasPolicy(FCPolicyDefOf.isolationist))
                isolationistTaxBoost = 10;

            foreach (ResourceType resourceType in ResourceUtils.resourceTypes)
            {
                //Grab trait additive variables
                int resourceMultiplier = 1;
                if (faction.hasPolicy(FCPolicyDefOf.technocratic) && resourceType == ResourceType.Research)
                    resourceMultiplier = 2;

                ResourceFC resource = getResource(resourceType);

                resource.baseProduction = biomeDef.BaseProductionAdditive[(int) resourceType] +
                                          hillinessDef.BaseProductionAdditive[(int) resourceType] +
                                          TraitUtilsFC.cycleTraits("productionBase" +
                                                                   resourceType, traits, Operation.Addition) +
                                          TraitUtilsFC.cycleTraits("productionBase" +
                                                                   resourceType, Find.World.GetComponent<FactionFC>().traits, Operation.Addition);
                resource.baseProductionMultiplier = resourceMultiplier *
                                                    biomeDef.BaseProductionMultiplicative[(int) resourceType] *
                                                    hillinessDef.BaseProductionMultiplicative[(int) resourceType] *
                                                    ((100 + egalitarianTaxBoost + isolationistTaxBoost + TraitUtilsFC.cycleTraits("taxBasePercentage", traits, Operation.Addition) + TraitUtilsFC.cycleTraits("taxBasePercentage", Find.World.GetComponent<FactionFC>().traits, Operation.Addition)) / 100);


                //add up additive variables
                double tempAdditive = 0;
                if (getResource(resourceType).baseProductionAdditives.Count() > 1
                ) //over one to skip null value (used to save in Expose)
                {
                    for (int k = 1; k < resource.baseProductionAdditives.Count(); k++)
                    {
                        tempAdditive += resource.baseProductionAdditives[k].value;
                    }
                }


                //multiply multiplicative variables
                double tempMultiplier = 1;
                if (resource.baseProductionMultipliers.Count() > 1
                ) //over one to skip null value (used to save in Expose
                {
                    for (int k = 1; k < resource.baseProductionMultipliers.Count(); k++)
                    {
                        tempMultiplier *= resource.baseProductionMultipliers[k].value;
                    }
                }


                //calculate end multiplier + endproduction and update to resource
                resource.endProductionMultiplier = resource.baseProductionMultiplier * (prosperity / 100) *
                                                   tempMultiplier *
                                                   TraitUtilsFC.cycleTraits("productionMultiplier" + resourceType,
                                                       traits, Operation.Multiplication) *
                                                   TraitUtilsFC.cycleTraits("productionMultiplier" + resourceType,
                                                       Find.World.GetComponent<FactionFC>().traits, Operation.Multiplication);
                resource.endProduction = resource.endProductionMultiplier *
                                         ((resource.baseProduction + tempAdditive) *
                                          resource.assignedWorkers);
            }
        }

        public double getTotalIncome() //return total income of settlements
        {
            double income = 0;
            foreach (ResourceType resourceType in ResourceUtils.resourceTypes)
            {
                ResourceFC resource = getResource(resourceType);
                if (resource != null)
                {
                    if (resource.isTithe == false)
                    {
                        //if resource is not paid by tithe
                        income += resource.endProduction * LoadedModManager.GetMod<FactionColoniesMod>()
                            .GetSettings<FactionColonies>().silverPerResource;
                    }
                }
            }

            //Log.Message("income " + income.ToString());
            return income;
        }

        public int getTotalWorkers()
        {
            int totalWorkers = 0;
            foreach (ResourceType resourceType in ResourceUtils.resourceTypes)
            {
                totalWorkers += getResource(resourceType).assignedWorkers;
            }

            if (totalWorkers > workersUltraMax)
            {
                while (totalWorkers > workersUltraMax)
                {
                    if (increaseWorkers(null, -1))
                    {
                        totalWorkers -= 1;
                    }

                    //Log.Message("Remove 1 worker");
                }
            }

            return totalWorkers;
        }

        private bool CanStillModify(ResourceType? resourceType, int singleMod) => workers + singleMod <= workersUltraMax && workers + singleMod >= 0 && getResource(resourceType.Value).assignedWorkers + singleMod <= workersUltraMax && getResource(resourceType.Value).assignedWorkers + singleMod >= 0;

        public bool increaseWorkers(ResourceType? resourceType, int numWorkers)
        {
            int singleMod = (numWorkers > 0) ?  1 : -1;
            if (resourceType == null)
            {
                if (numWorkers >= 0 && workers <= workersUltraMax)
                {
                    return false;
                }

                while (workers > workersUltraMax)
                {
                    int num = Rand.RangeInclusive(0, ResourceUtils.resourceTypes.Length - 1);
                    //Log.Message(num.ToString());
                    if (getResource(ResourceUtils.resourceTypes[num]).assignedWorkers > 0)
                    {
                        getResource(ResourceUtils.resourceTypes[num]).assignedWorkers -= 1;
                        return true;
                    }
                }
            }
            else while(CanStillModify(resourceType, singleMod))
            {
                workers += singleMod;
                getResource(resourceType.Value).assignedWorkers += singleMod;
                numWorkers -= singleMod;
                updateProfitAndProduction();
                Find.World.GetComponent<FactionFC>().updateTotalProfit();
                if (numWorkers == 0) return true;
            }

            return false;
        }

        public double getBaseWorkerCost()
        {
            return (LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().workerCost +
                    (TraitUtilsFC.cycleTraits("workerBaseCost", traits, Operation.Addition) +
                     TraitUtilsFC.cycleTraits("workerBaseCost",
                         Find.World.GetComponent<FactionFC>().traits, Operation.Addition)));
            //add building/faction modifierse
        }

        public double getTotalUpkeep() //returns total upkeep of all settlements
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            workers = getTotalWorkers();
            double upkeep = 0;
            double overWork;
            if (workers > workersMax)
            {
                overWork = (int) (workers - workersMax);
            }
            else
            {
                overWork = 0;
            }

            workerTotalUpkeep = (workers * getBaseWorkerCost()) + ((workers * getBaseWorkerCost()) * (overWork / 20));

            //add building upkeep

            upkeep += (workerTotalUpkeep);


            foreach (BuildingFCDef building in buildings)
            {
                bool isMilitary = false;
                foreach (FCTraitEffectDef trait in building.traits)
                {
                    if (trait.militaryBaseLevel > 0)
                        isMilitary = true;
                    if (trait.militaryMultiplierCombatEfficiency > 1)
                    {
                        isMilitary = true;
                    }
                }

                if (building.upkeep != 0 && !isMilitary || !faction.hasPolicy(FCPolicyDefOf.militaristic))
                    upkeep += building.upkeep;
                else
                    upkeep += Math.Max(0, building.upkeep - 100);
            }

            //Log.Message("upkeep " + upkeep.ToString());
            return upkeep;
        }

        public void updateWorkerCost() //runs inside updateProfit to attach during updating
        {
            workerCost = (workerTotalUpkeep / workers);
        }

        public double getTotalProfit() //returns total profit (income - upkeep) of all settlements
        {
            return (getTotalIncome() - getTotalUpkeep());
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref worldSettlement, "worldSettlement");
            Scribe_Values.Look(ref mapLocation, "mapLocation");
            Scribe_Values.Look(ref planetName, "planetName");
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref nameShort, "nameShort", ShortName);
            Scribe_Values.Look(ref nameOriginal, "nameOriginal", OriginalName);
            Scribe_Values.Look(ref loadID, "loadID", -1);
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref productionEfficiency, "productionEfficiency");
            Scribe_Values.Look(ref workers, "workers");
            Scribe_Values.Look(ref workersMax, "workersMax");
            Scribe_Values.Look(ref workersUltraMax, "workersUltraMax");
            Scribe_Values.Look(ref settlementLevel, "settlementLevel");
            Scribe_Values.Look(ref settlementMilitaryLevel, "settlementMilitaryLevel");
            Scribe_Values.Look(ref unrest, "unrest");
            Scribe_Values.Look(ref loyalty, "loyalty");
            Scribe_Values.Look(ref happiness, "happiness");
            Scribe_Values.Look(ref prosperity, "prosperity");
            Scribe_Values.Look(ref workerCost, "workerCost");
            Scribe_Values.Look(ref workerTotalUpkeep, "workerTotalUpkeep");


            //save resources
            Scribe_Deep.Look(ref food, "food");
            Scribe_Deep.Look(ref weapons, "weapons");
            Scribe_Deep.Look(ref apparel, "apparel");
            Scribe_Deep.Look(ref animals, "animals");
            Scribe_Deep.Look(ref logging, "logging");
            Scribe_Deep.Look(ref mining, "mining");
            Scribe_Deep.Look(ref research, "research");
            Scribe_Deep.Look(ref power, "power");
            Scribe_Deep.Look(ref medicine, "medicine");


            //Taxes
            Scribe_Collections.Look(ref buildings, "buildings", LookMode.Def);
            Scribe_Collections.Look(ref tithe, "tithe", LookMode.Deep);
            Scribe_Values.Look(ref titheEstimatedIncome, "titheEstimatedIncome");
            Scribe_Values.Look(ref silverIncome, "silverIncome");


            //Traits
            Scribe_Collections.Look(ref traits, "traits", LookMode.Def);

            //Biome_info
            Scribe_Values.Look(ref hilliness, "hilliness");
            Scribe_Values.Look(ref biome, "biome");
            Scribe_Defs.Look(ref hillinessDef, "hillinessdef");
            Scribe_Defs.Look(ref biomeDef, "biomedef");


            //Military
            Scribe_Values.Look(ref militaryBusy, "militaryBusy");
            Scribe_Values.Look(ref militaryLocation, "militaryLocation");
            Scribe_Values.Look(ref militaryJob, "militaryJob");
            Scribe_References.Look(ref militaryEnemy, "militaryEnemy");
            Scribe_Values.Look(ref isUnderAttack, "isUnderAttack");
            Scribe_References.Look(ref militarySquad, "militarySquad");
            Scribe_Values.Look(ref artilleryTimer, "artilleryTimer");
            Scribe_Values.Look(ref militaryLocationPlanet, "militaryLocationPlanet");
            Scribe_Values.Look(ref autoDefend, "autoDefend");


            //Prisoners
            Scribe_Collections.Look(ref prisonerList, "prisonerList", LookMode.Deep);

            //Traits
            Scribe_Values.Look(ref trait_Egalitarian_TaxBreak_Tick, "trait_Egalitarian_TaxBreak_Tick");
            Scribe_Values.Look(ref trait_Egalitarian_TaxBreak_Enabled, "trait_Egalitarian_TaxBreak_Enabled");

            //Shuttles
            Scribe_Values.Look(ref lastShuttleUsesTick, "lastShuttleUsesTick");
        }

        //Settlement Base Info
        public int mapLocation;
        public string planetName;
        public string name;
        private string nameShort;
        private string nameOriginal;
        public int loadID;
        public string title = "Hamlet".Translate();
        public string description = "FCGenericError".Translate();
        public double workers;
        public double workersMax;
        public double workersUltraMax;
        public double workerCost;
        public double workerTotalUpkeep;
        public int settlementLevel = 1;
        public int settlementMilitaryLevel;
        public double unrest;
        public double loyalty = 100;
        public double happiness = 100;
        public double prosperity = 100;
        public List<BuildingFCDef> buildings = new List<BuildingFCDef>();
        public List<FCTraitEffectDef> traits = new List<FCTraitEffectDef>();
        public List<FCPrisoner> prisonerList = new List<FCPrisoner>();

        public float silverIncome;
        public List<Thing> tithe = new List<Thing>();
        public int titheEstimatedIncome;

        public string hilliness;
        public string biome;
        public BiomeResourceDef hillinessDef;
        public BiomeResourceDef biomeDef;


        //ui only
        public double totalUpkeep;
        public double totalIncome;
        public double totalProfit;


        //Military stuff
        public bool militaryBusy;
        public int militaryLocation = -1;
        public string militaryLocationPlanet;
        public MilitaryJob militaryJob = MilitaryJob.Undefined;
        public Faction militaryEnemy;
        public bool isUnderAttack;
        public MercenarySquadFC militarySquad;
        public int artilleryTimer = 0;
        public bool autoDefend = false;

        //Trait stuff
        public int trait_Egalitarian_TaxBreak_Tick;
        public bool trait_Egalitarian_TaxBreak_Enabled;

        //shuttle stuff
        public int lastShuttleUsesTick = 0;


        //public static Biome biome;

        //Settlement Production Information
        public double productionEfficiency; //Between 0.1 - 1

        public string ShortName
        {
            get
            {
                if (!nameShort.NullOrEmpty()) return nameShort;

                nameShort = TextGen.ToShortName(name);

                return nameShort;
            }
            set => nameShort = value.NullOrEmpty() ? name : value;
        }

        public string OriginalName
        {
            get => nameOriginal;
            private set => nameOriginal = value;
        }

        public bool isMilitaryBusy(bool silent = false)
        {
            if (militaryBusy && !silent)
            {
                Messages.Message("militaryAlreadyAssigned".Translate(), MessageTypeDefOf.RejectInput);
            }

            return militaryBusy;
        }

        public bool isMilitarySquadValid()
        {
            if (militarySquad != null)
            {
                if (militarySquad.outfit != null)
                {
                    if (militarySquad.EquippedMercenaries.Count > 0)
                    {
                        return true;
                    }

                    Messages.Message("You can't deploy a squad with no equipped personnel!",
                        MessageTypeDefOf.RejectInput);
                    return false;
                }

                Messages.Message("There is no squad loadout assigned to that settlement!",
                    MessageTypeDefOf.RejectInput);
                return false;
            }

            Messages.Message("There is no military squad assigned to that settlement!", MessageTypeDefOf.RejectInput);
            return false;
        }

        public bool isMilitarySquadValidSilent()
        {
            if (militarySquad != null)
            {
                return true;
            }

            return false;
        }

        public bool isMilitaryBusySilent()
        {
            return militaryBusy;
        }
        /// <summary>
        /// Compatibility focused: this object should only be destroyed very deliberately, else another object is likely trying to handle negative combat resolution against this settlement.
        /// </summary>
        public void PrepareDestroyWorldObject()
        {
            worldSettlement.PrepareDestroy();
        }

        public bool isMilitaryValid()
        {
            if (settlementMilitaryLevel > 0)
            {
                //if settlement military is more than level 0
                return true;
            }

            return false;
        }

        public bool isTargetOccupied(int location)
        {
            if (Find.World.GetComponent<FactionFC>().militaryTargets.Contains(location))
            {
                Messages.Message("targetAlreadyBeingAttacked".Translate(), MessageTypeDefOf.RejectInput);
                return true;
            }

            return false;
        }

        public float Happiness
        {
            get { return (float) Math.Round(happiness, 1); }
        }

        public float Unrest
        {
            get { return (float) Math.Round(unrest, 1); }
        }

        public float Loyalty
        {
            get { return (float) Math.Round(loyalty, 1); }
        }

        public float Prosperity
        {
            get { return (float) Math.Round(prosperity, 1); }
        }

        public void SendMilitary(int location, string planet, MilitaryJob job, int timeToFinish, Faction enemy)
        {
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();
            if (isMilitaryBusy() || isTargetOccupied(location)) return;
           
            militaryBusy = true;
            militaryJob = job;
            militaryLocationPlanet = planet;
            militaryLocation = location;

            if (enemy != null) militaryEnemy = enemy;
            if (job != MilitaryJob.Deploy) Find.World.GetComponent<FactionFC>().militaryTargets.Add(location);

            FCEvent evt;
            switch (militaryJob)
            {
                case MilitaryJob.RaidEnemySettlement:
                    evt = FCEventMaker.MakeEvent(FCEventDefOf.raidEnemySettlement);
                    evt.customDescription = "settlementMilitaryForcesRaiding".Translate(name, returnMilitaryTarget().Label);
                    Find.LetterStack.ReceiveLetter("FCMilitaryAction".Translate(), "FCMilitarySentRaid".Translate(name, Find.WorldObjects.SettlementAt(location)), LetterDefOf.NeutralEvent);
                    evt.DefineEvent(factionfc, mapLocation, timeToFinish);
                    break;

                case MilitaryJob.EnslaveEnemySettlement:
                    evt = FCEventMaker.MakeEvent(FCEventDefOf.enslaveEnemySettlement);
                    evt.customDescription ="settlementMilitaryForcesEnslave".Translate(name, returnMilitaryTarget().Label);
                    Find.LetterStack.ReceiveLetter("FCMilitaryAction".Translate(), "FCMilitarySentEnslave".Translate(name, Find.WorldObjects.SettlementAt(location)), LetterDefOf.NeutralEvent);
                    evt.DefineEvent(factionfc, mapLocation, timeToFinish);
                    break;

                case MilitaryJob.CaptureEnemySettlement:
                    evt = FCEventMaker.MakeEvent(FCEventDefOf.captureEnemySettlement);
                    evt.customDescription ="settlementMilitaryForcesCapturing".Translate(name, returnMilitaryTarget().Label);
                    Find.LetterStack.ReceiveLetter("FCMilitaryAction".Translate(), "FCMilitarySentCapture".Translate(name, Find.WorldObjects.SettlementAt(location)), LetterDefOf.NeutralEvent);
                    evt.DefineEvent(factionfc, mapLocation, timeToFinish);
                    break;

                default:
                    break;
            }
        }

        public Settlement returnMilitaryTarget()
        {
            return militaryLocation == -1 ? null : Find.WorldObjects.SettlementAt(militaryLocation);
        }


        //CURRENTLY NOT USED
        public void militaryTick()
        {
            if (militaryBusy == false)
            {
                //if not busy
            }
        }

        public void processMilitaryEvent()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            //calculate success and all of that shit

            //Debug by setting faction automatically
            //returnMilitaryTarget().SetFaction(FactionColonies.getPlayerColonyFaction());
            if (faction.militaryTargets.Contains(militaryLocation))
            {
                faction.militaryTargets.Remove(militaryLocation);
            }
            //Log.Message(winner + " job = " + militaryJob);
            //Process end result here
            //attacker == 0; defender == 1;

            switch (militaryJob)
            {
                case MilitaryJob.RaidEnemySettlement:
                {
                    int winner = SimulateBattleFc.FightBattle(militaryForce.createMilitaryForceFromSettlement(this, true),
                        militaryForce.createMilitaryForceFromFaction(militaryEnemy, false));
                    if (winner == 0)
                    {
                        //if won
                        faction.addExperienceToFactionLevel(5f);

                        TechLevel tech = Find.WorldObjects.SettlementAt(militaryLocation).Faction.def.techLevel;
                        int lootLevel;
                        bool getSlaves = true;


                        switch (tech)
                        {
                            case TechLevel.Archotech:
                            case TechLevel.Ultra:
                            case TechLevel.Spacer:
                                lootLevel = 4;
                                break;
                            case TechLevel.Industrial:
                                lootLevel = 3;
                                break;
                            case TechLevel.Medieval:
                            case TechLevel.Neolithic:
                                lootLevel = 2;
                                break;
                            default:
                                lootLevel = 1;
                                break;
                        }

                        if (Find.WorldObjects.SettlementAt(militaryLocation).Faction.def.defName == "VFEI_Insect")
                        {
                            lootLevel = 3;
                            getSlaves = false;
                        }

                        List<Thing> loot = PaymentUtil.generateRaidLoot(lootLevel, tech);

                        string text = "settlementDeliveringLoot".Translate();
                        text = loot.Aggregate(text, (current, thing) => current + thing.LabelCap + " " + thing.stackCount + "x\n ");

                        int num = new IntRange(0, 10).RandomInRange;
                        if (num <= 4 && getSlaves)
                        {
                            Pawn prisoner = PaymentUtil.generatePrisoner(militaryEnemy);
                            text += "PrisonerCaptureInfo".Translate(prisoner.Name.ToString(), name);
                            addPrisoner(prisoner);
                        }

                        Find.LetterStack.ReceiveLetter("RaidLoot".Translate(),
                            "RaidEnemySettlementSuccess".Translate(
                                Find.WorldObjects.SettlementAt(militaryLocation).LabelCap) + "\n" + text,
                            LetterDefOf.PositiveEvent, new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));

                            //deliver

                            FCEvent eventParams = new FCEvent()
                            {
                                location = Find.AnyPlayerHomeMap.Tile,
                                source = mapLocation,
                                planetName = planetName,
                                goods = loot,
                                customDescription = text,
                                timeTillTrigger = Find.TickManager.TicksGame + FactionColonies.ReturnTicksToArrive(mapLocation, Find.AnyPlayerHomeMap.Tile)
                            };

                            DeliveryEvent.CreateDeliveryEvent(eventParams);
                    }
                    else
                    {
                        //if lost
                        Find.LetterStack.ReceiveLetter("RaidFailure".Translate(),
                            "RaidEnemySettlementFailure".Translate(
                                Find.WorldObjects.SettlementAt(militaryLocation).LabelCap), LetterDefOf.NegativeEvent,
                            new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));
                    }

                    break;
                }
                case MilitaryJob.EnslaveEnemySettlement:
                {
                    int winner = SimulateBattleFc.FightBattle(militaryForce.createMilitaryForceFromSettlement(this, true),
                        militaryForce.createMilitaryForceFromFaction(militaryEnemy, false));
                    if (winner == 0)
                    {
                        //if won
                        faction.addExperienceToFactionLevel(5f);

                        string text = "";

                        int num = new IntRange(1, 3).RandomInRange;
                        for (int i = 0; i <= num; i++)
                        {
                            Pawn prisoner = PaymentUtil.generatePrisoner(militaryEnemy);
                            text += "PrisonerCaptureInfo".Translate(prisoner.Name.ToString(), name) + "\n";
                            addPrisoner(prisoner);
                        }

                        Find.LetterStack.ReceiveLetter("RaidLoot".Translate(),
                            "RaidEnemySettlementSuccess".Translate(
                                Find.WorldObjects.SettlementAt(militaryLocation).LabelCap) + "\n" + text,
                            LetterDefOf.PositiveEvent, new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));
                        }
                    else if (winner == 1)
                    {
                        //if lost
                        Find.LetterStack.ReceiveLetter("RaidFailure".Translate(),
                            "RaidEnemySettlementFailure".Translate(
                                Find.WorldObjects.SettlementAt(militaryLocation).LabelCap), LetterDefOf.NegativeEvent,
                            new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));
                    }

                    break;
                }
                case MilitaryJob.CaptureEnemySettlement:
                {
                    int winner = SimulateBattleFc.FightBattle(militaryForce.createMilitaryForceFromSettlement(this, true),
                        militaryForce.createMilitaryForceFromFaction(militaryEnemy, false));
                    if (winner == 0)
                    {
                        //Log.Message("Won");
                        faction.addExperienceToFactionLevel(5f);
                    
                        string tmpName = Find.WorldObjects.SettlementAt(militaryLocation).LabelCap;
                        TechLevel tech = Find.WorldObjects.SettlementAt(militaryLocation).Faction.def.techLevel;
                        Faction tempFactionLink = Find.WorldObjects.SettlementAt(militaryLocation).Faction;
                        Find.WorldObjects.SettlementAt(militaryLocation).Destroy();
                        if (Find.World.info.name == militaryLocationPlanet)
                        {
                            WorldSettlementFC settlement =
                                FactionColonies.createPlayerColonySettlement(militaryLocation, true,
                                    militaryLocationPlanet);
                            settlement.Name = tmpName;
                        }
                        else
                        {
                            FactionColonies.createPlayerColonySettlement(militaryLocation, false,
                                militaryLocationPlanet);
                            Find.World.GetComponent<FactionFC>().createSettlementQueue
                                .Add(new SettlementSoS2Info(militaryLocationPlanet, militaryLocation));
                        }

                        SettlementFC settlementFc = Find.World.GetComponent<FactionFC>()
                            .returnSettlementByLocation(militaryLocation, Find.World.info.name);
                        settlementFc.name = tmpName;

                        int upgradeTimes;

                        switch (tech)
                        {
                            case TechLevel.Archotech:
                            case TechLevel.Ultra:
                            case TechLevel.Spacer:
                                upgradeTimes = 2;
                                break;
                            case TechLevel.Industrial:
                                upgradeTimes = 1;
                                break;
                            default:
                                upgradeTimes = 0;
                                break;
                        }
                        
                        settlementFc.upgradeSettlement(upgradeTimes);

                        settlementFc.loyalty = 15;
                        settlementFc.happiness = 25;
                        settlementFc.unrest = 20;
                        settlementFc.prosperity = 70;

                        bool defeated = !Find.WorldObjects.Settlements.Any(settlement => settlement.Faction != null
                            && settlement.Faction == tempFactionLink);

                        if (defeated)
                        {
                            tempFactionLink.defeated = true;
                        }

                        Find.LetterStack.ReceiveLetter("CaptureSettlement".Translate(),
                            "CaptureEnemySettlementSuccess".Translate(name,
                                Find.WorldObjects.SettlementAt(militaryLocation).Name, settlementFc.settlementLevel),
                            LetterDefOf.PositiveEvent, new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));
                    }
                    else if (winner == 1)
                    {
                        //Log.Message("Loss");
                        Find.LetterStack.ReceiveLetter("CaptureSettlement".Translate(),
                            "CaptureEnemySettlementFailure".Translate(name,
                                Find.WorldObjects.SettlementAt(militaryLocation).Name), LetterDefOf.NegativeEvent,
                            new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));
                    }

                    break;
                }
            }

            cooldownMilitary();
        }

        public void returnMilitary(bool alert)
        {
            militaryBusy = false;
            militaryJob = MilitaryJob.Undefined;
            militaryLocation = -1;
            militaryEnemy = null;

            if (alert)
            {
                Find.LetterStack.ReceiveLetter("Military Cooldown", "FCMilitaryCooldown".Translate(name),
                    LetterDefOf.PositiveEvent);
            }
        }

        public void cooldownMilitary()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();

            int cooldownReduction = 0;
            if (faction.hasTrait(FCPolicyDefOf.raiders) && (militaryJob == MilitaryJob.RaidEnemySettlement || militaryJob == MilitaryJob.EnslaveEnemySettlement))
            {
                cooldownReduction += 60000;
            }
            else if (militaryJob == MilitaryJob.Deploy && FactionColonies.Settings().deadPawnsIncreaseMilitaryCooldown)
            {
                List<string> policies = faction.policies.ConvertAll(policy => policy.def.defName);
                bool militarist = policies.Contains("militaristic");
                bool authoritarian = policies.Contains("authoritarian");
                bool pacifist = policies.Contains("pacifist");

                int deadMultiplier = (militarist || authoritarian ? militarist && authoritarian ? 7000 : 8000 : 10000) + (pacifist ? 2000 : 0);

                cooldownReduction -= militarySquad.dead * deadMultiplier;
            }

            militaryJob = MilitaryJob.Cooldown;
            militaryBusy = true;
            militaryLocation = mapLocation;
            militaryEnemy = null;

            FCEvent tmp = FCEventMaker.MakeEvent(FCEventDefOf.cooldownMilitary);
            tmp.hasCustomDescription = true;
            tmp.timeTillTrigger = Find.TickManager.TicksGame + 180000 - cooldownReduction;
            tmp.location = mapLocation;
            tmp.planetName = planetName;
            tmp.customDescription = "MilitaryForcesReorganizing".Translate(name); // + 
            Find.World.GetComponent<FactionFC>().addEvent(tmp);
        }

        public ResourceFC returnHighestResource()
        {
            double highest = -1;
            ResourceFC highestResource = null;

            foreach (ResourceType resourceType in ResourceUtils.resourceTypes)
            {
                ResourceFC resource = getResource(resourceType);
                if (resource.endProduction > highest)
                {
                    highest = resource.endProduction;
                    highestResource = resource;
                }
            }

            return highestResource;
        }

        public void updateDescription()
        {
            //biome

            switch (biomeDef.defName)
            {
                case "BorealForest":
                    description = "FCDescBorealForest".Translate();
                    break;
                case "Tundra":
                    description = "FCDescTundra".Translate();
                    break;
                case "ColdBog":
                    description = "FCDescColdBog".Translate();
                    break;
                case "IceSheet":
                    description = "FCDescIceSheet".Translate();
                    break;
                case "SeaIce":
                    description = "FCDescIceSheet".Translate();
                    break;
                case "TemperateForest":
                    description = "FCDescTemperateForest".Translate();
                    break;
                case "TemperateSwamp":
                    description = "FCDescTemperateSwamp".Translate();
                    break;
                case "TropicalRainforest":
                    description = "FCDescTropicalRainforest".Translate();
                    break;
                case "AridShrubland":
                    description = "FCDescAridShrubland".Translate();
                    break;
                case "Desert":
                    description = "FCDescDesert".Translate();
                    break;
                case "ExtremeDesert":
                    description = "FCDescExtremeDesert".Translate();
                    break;
                default:
                    description = "FCDescUnknown".Translate();
                    break;
            }


            //town size

            switch (settlementLevel)
            {
                case 1:
                    description += "FCTownLevel1".Translate();
                    break;
                case 2:
                    description += "FCTownLevel2".Translate();
                    break;
                case 3:
                case 4:
                    description += "FCTownLevel3".Translate();
                    break;
                case 5:
                case 6:
                    description += "FCTownLevel4".Translate();
                    break;
                case 7:
                case 8:
                default:
                    description += "FCTownLevel5".Translate();
                    break;
            }
        }

        public List<FCTraitEffectDef> returnListSettlementTraits()
        {
            List<FCTraitEffectDef> tmpList = new List<FCTraitEffectDef>();
            foreach (FCTraitEffectDef trait in traits)
            {
                tmpList.Add(trait);
            }

            return tmpList;
        }

        public void deconstructBuilding(int buildingSlot)
        {
            foreach (FCTraitEffectDef trait in buildings[buildingSlot].traits) //remove traits
            {
                foreach (FCTraitEffectDef settlement in traits)
                {
                    if (settlement == trait)
                    {
                        traits.Remove(settlement);
                        break;
                    }
                }
            }

            buildings[buildingSlot] = BuildingFCDefOf.Empty;
        }

        private int returnMaxWorkersFromPrisoners()
        {
            int num = 0;
            foreach (FCPrisoner prisoner in prisonerList)
            {
                switch (prisoner.workload)
                {
                    case FCWorkLoad.Medium:
                        num++;
                        break;
                    case FCWorkLoad.Heavy:
                        num += 2;
                        break;
                }
            }

            return num;
        }

        private int returnOverMaxWorkersFromPrisoners()
        {
            //Log.Message("max worker : " + num);
            return prisonerList.Count(prisoner => prisoner.workload == FCWorkLoad.Light);
        }


        public bool validConstructBuilding(BuildingFCDef building, int buildingSlot, SettlementFC settlement)
        {
            bool valid = true;
            foreach (BuildingFCDef slot in buildings) //check if already a building of that type constructed
            {
                if (slot == building)
                {
                    valid = false;
                    Messages.Message("BuildingAlreadyType".Translate() + "!", MessageTypeDefOf.RejectInput);
                    break;
                }
            }

            if (PaymentUtil.getSilver() < building.cost) //check if the player has enough money
            {
                valid = false;
                Messages.Message("NotEnoughSilverConstructBuilding".Translate() + "!", MessageTypeDefOf.RejectInput);
            }

            foreach (FCEvent event1 in Find.World.GetComponent<FactionFC>().events) //check if construction would match any already-occuring events
            {
                if (isUnderAttack)
                {
                    valid = false;
                    Messages.Message("SettlementUnderAttack".Translate(), MessageTypeDefOf.RejectInput);
                }
                if (event1.source == mapLocation && event1.building == building &&
                    event1.def.defName == "constructBuilding")
                {
                    valid = false;
                    Messages.Message("BuildingBeingBuiltAlreadyType".Translate() + "!", MessageTypeDefOf.RejectInput);
                    break;
                }

                if (event1.source == mapLocation && event1.buildingSlot == buildingSlot &&
                    event1.def.defName == "constructBuilding"
                ) //check if there is already a building being constructed in that slot
                {
                    valid = false;
                    Messages.Message("BuildingAlreadyConstructed".Translate() + "!", MessageTypeDefOf.RejectInput);
                    break;
                }
            }

            if (building.applicableBiomes.Count != 0)
            {
                bool match = building.applicableBiomes.Contains(settlement.biome);

                //if found no matches
                if (match == false)
                {
                    valid = false;
                    Messages.Message("BuildingInvalidEnvironment".Translate(), MessageTypeDefOf.RejectInput);
                }
            }

            return valid;
        }


        public void constructBuilding(BuildingFCDef building, int buildingSlot)
        {
            deconstructBuilding(buildingSlot);

            buildings[buildingSlot] = building;

            traits.AddRange(building.traits); //add new traits
        }


        //Reference
        //0 - settlement name
        //1 - food end production
        //2 - weapon end pro
        //3 - apparel end pro
        //4 - animals end pro
        //5 - logging end pro
        //6 - mining end pro
        //7 - report button
        //8 - tithe est value
        //9 - Silver income
        //10 - location id


        public double returnTitheEstimatedValue()
        {
            double titheVal = 0;
            foreach (ResourceType resourceType in ResourceUtils.resourceTypes)
            {
                if (getResource(resourceType).isTithe)
                {
                    titheVal += getResource(resourceType).endProduction * LoadedModManager.GetMod<FactionColoniesMod>()
                        .GetSettings<FactionColonies>().silverPerResource;
                }
            }

            return titheVal;
        }

        public ResourceFC returnResource(string name) //used to return the correct resource based on string name
        {
            switch (name)
            {
                case "food":
                    return food;
                case "weapons":
                    return weapons;
                case "apparel":
                    return apparel;
                case "animals":
                    return animals;
                case "logging":
                    return logging;
                case "mining":
                    return mining;
                case "research":
                    return research;
                case "power":
                    return power;
                case "medicine":
                    return medicine;
                default:
                    Log.Message("Unable to find resource - returnResource(string name)");
                    return null;
            }
        }

        public ResourceFC getResource(ResourceType type) //used to return the correct resource based on string name
        {
            switch (type)
            {
                case ResourceType.Food:
                    return food;
                case ResourceType.Weapons:
                    return weapons;
                case ResourceType.Apparel:
                    return apparel;
                case ResourceType.Animals:
                    return animals;
                case ResourceType.Logging:
                    return logging;
                case ResourceType.Mining:
                    return mining;
                case ResourceType.Research:
                    return research;
                case ResourceType.Power:
                    return power;
                case ResourceType.Medicine:
                    return medicine;
                default:
                    Log.Message("Unable to find resource - returnResourceByInt(int name)");
                    return null;
            }
        }

        public string returnResourceNameByInt(int name) //used to return the correct resource based on string name
        {
            if (name == 0)
            {
                return "Food";
            }

            if (name == 1)
            {
                return "Weapons";
            }

            if (name == 2)
            {
                return "Apparel";
            }

            if (name == 3)
            {
                return "Animals";
            }

            if (name == 4)
            {
                return "Logging";
            }

            if (name == 5)
            {
                return "Mining";
            }

            if (name == 6)
            {
                return "Research";
            }

            if (name == 7)
            {
                return "Power";
            }

            if (name == 8)
            {
                return "Medicine";
            }

            Log.Message("Unable to find resource - returnResourceByInt(int name)");
            return null;
        }

        //Settlment resources
        public ResourceFC food = new ResourceFC(0, ResourceType.Food);
        public ResourceFC weapons = new ResourceFC(0, ResourceType.Weapons);
        public ResourceFC apparel = new ResourceFC(0, ResourceType.Apparel);
        public ResourceFC animals = new ResourceFC(0, ResourceType.Animals);
        public ResourceFC logging = new ResourceFC(0, ResourceType.Logging);
        public ResourceFC mining = new ResourceFC(0, ResourceType.Mining);
        public ResourceFC power = new ResourceFC(0, ResourceType.Power);
        public ResourceFC medicine = new ResourceFC(0, ResourceType.Medicine);
        public ResourceFC research = new ResourceFC(0, ResourceType.Research);


        public void taxProductionGoods() //update goods (TAX TAX TAX)   # Not used?
        {
            int silver = 0;
            foreach (ResourceType type in ResourceUtils.resourceTypes)
            {
                ResourceFC resource = getResource(type);
                if (resource.isTithe)
                {
                    //if resource is paying via tithe
                    //generate the tithe things
                    //run tithe cash evaluation here
                    //ThingSetMaker gen = new ThingSetMaker();
                }
                else
                {
                    //if resource is paying via silver
                    silver += (int) (resource.endProduction * LoadedModManager.GetMod<FactionColoniesMod>()
                        .GetSettings<FactionColonies>().silverPerResource); //Add randomness?
                }
            }
        }

        //UNUSED FUNCTIONS
        public float getSilverIncome()
        {
            return silverIncome;
        }

        public void resetSilverIncome()
        {
            silverIncome = 0;
        }

        public void addSilverIncome(float amount)
        {
            silverIncome += amount;
        }

        public float returnSilverIncome(bool reset)
        {
            float income = silverIncome;

            if (reset)
            {
                resetSilverIncome();
            }

            return income;
        }

        public List<Thing> getTithe()
        {
            return tithe;
        }

        public void resetTithe()
        {
            tithe = new List<Thing>();
        }
        //UNUSED FUNCTIONS /END

        public void goTo()
        {
            Find.World.renderer.wantedMode = WorldRenderMode.Planet;

            //Select Settlement Tile
            //Find.WorldObjects.AnySettlementAt(settlementList[i][10])
            Find.WorldSelector.ClearSelection();
            Find.WorldSelector.Select(
                Find.WorldObjects
                    .MapParentAt(
                        mapLocation)); // = Convert.ToInt32(settlementList[i][10]); //(Find.World.GetComponent<FactionFC>().settlements[i]);
            if (Find.MainButtonsRoot.tabs.OpenTab != null)
            {
                Find.MainButtonsRoot.tabs.OpenTab.TabWindow.Close();
            }
        }

        public float createResearchPool()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();

            double production = research.endProduction;
            double innovativeBonusResearch = 0;
            double technocraticModifier = 1;
            if (faction.hasPolicy(FCPolicyDefOf.technocratic))
                technocraticModifier = 2;
            if (faction.hasTrait(FCPolicyDefOf.innovative))
                innovativeBonusResearch = (getTotalProfit() * .05) * technocraticModifier;
            return (float) Math.Max(
                Math.Round((production * FactionColonies.productionResearchBase) + innovativeBonusResearch), 0);
        }

        public float createPowerPool()
        {
            double allotted = power.endProduction;
            return (float) Math.Round(allotted * 100);
        }

        public List<Thing> createTithe(float industriousTaxPercentageBoost)
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();


            List<Thing> list = new List<Thing>();
            foreach (ResourceType resourceType in ResourceUtils.resourceTypes)
            {
                ResourceFC resource = getResource(resourceType);
                if (resource.isTithe && resourceType != ResourceType.Power && resourceType != ResourceType.Research)
                {
                    if (resource.filter == null)
                    {
                        resource.filter = new ThingFilter();
                        PaymentUtil.resetThingFilter(this, resourceType);
                    }

                    if (!resource.filter.AllowedThingDefs.Any())
                    {
                        Find.LetterStack.ReceiveLetter("No Tithe",
                            "There are no enabled items in the tithe" + resource + " of settlement " +
                            name, LetterDefOf.NegativeEvent);
                        continue;
                    }

                    List<Thing> tmpList;

                    double production = resource.endProduction;
                    production *= industriousTaxPercentageBoost * ((100 + TraitUtilsFC.cycleTraits("taxBasePercentage", traits, Operation.Addition) + TraitUtilsFC.cycleTraits("taxBasePercentage", Find.World.GetComponent<FactionFC>().traits, Operation.Addition)) / 100);
                    int assignedWorkers = resource.assignedWorkers;

                    //Create Temp Value
                    double tmpValue = production * LoadedModManager.GetMod<FactionColoniesMod>()
                        .GetSettings<FactionColonies>().silverPerResource;
                    resource.taxStock += tmpValue;
                    resource.returnLowestCost();
                    if (resource.checkMinimum())
                    {
                        if (faction.hasPolicy(FCPolicyDefOf.feudal))
                            resource.taxStock *= 1.2;
                        tmpList = PaymentUtil.generateTithe(resource.taxStock,
                            LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>()
                                .productionTitheMod, assignedWorkers, resourceType,
                            TraitUtilsFC.cycleTraits("taxBaseRandomModifier",
                                Find.World.GetComponent<FactionFC>().traits, Operation.Addition) +
                            TraitUtilsFC.cycleTraits("taxBaseRandomModifier", traits, Operation.Addition), this);

                        foreach (Thing thing in tmpList)
                        {
                            list.Add(thing);
                        }

                        resource.taxStock = 0;
                    }

                    resource.returnTaxPercentage();
                }
            }

            return list;
        }
    }
}