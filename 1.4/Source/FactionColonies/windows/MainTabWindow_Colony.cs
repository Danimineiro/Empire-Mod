using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace FactionColonies
{
	public class MainTabWindow_Colony : MainTabWindow
	{
		public bool selectingColonyFC;
		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(350f, 400f);
			}
		}

		public void MainTabWindow()
		{
			closeOnClickedOutside = false;
		}



		public int tab;
		public int tabSize = 78;
		public int resourceSize;
		public FactionFC faction;
		public List<SettlementFC> settlementList;
		public int scroll;
		public int maxScroll;

		private readonly int xspacing = 45;
		private readonly int yspacing = 30;
		private readonly int yoffset = 100;
		private readonly int headerSpacing = 30;

		private List<string> stats = new List<string>(); //button list to draw
		private int statSize = 25; // height size of the stats when drawing
		private List<string> buttons = new List<string>(); //button list to draw
		private readonly int buttonSize = 25; // height size of the buttons when drawing


		private readonly List<string> statsTab0 = new List<string> {"happiness", "loyalty", "unrest", "prosperity"};

		private readonly List<string> buttonsTab0 = new List<string> { "FCOverview".Translate(), "Military".Translate(), "Actions".Translate()};
		
		public override void PreOpen()
		{
			base.PreOpen();
			stats = statsTab0;
			statSize = 25;
			buttons = buttonsTab0;
			resourceSize = 40;
			faction = Find.World.GetComponent<FactionFC>();
			if (faction != null)
			{
				settlementList = faction.settlements;
				faction.updateAverages();

				//Initial release - Autocreate faction
				//Faction faction = FactionColonies.getPlayerColonyFaction();
				//if (faction == null)
				//{
				//	FactionColonies.createPlayerColonyFaction();
				//}

				//if (faction.capitalLocation == -1)
				//{
				//	faction.setCapital();
				//}

				faction.updateTotalProfit();
			}
			else
			{
				Log.Message("WorldComp FactionFC is null - Something is wrong! Empire Mod");
			}
			
		}

		public override void PostClose()
		{
			base.PostClose();
			//If selecting colony
			selectingColonyFC = false;


		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
			UiUpdate();
		}

		//UI STUFF
		//time variables
		private int UIUpdateTimer;

		public void WindowUpdateFC()
		{
			faction.updateAverages();
			maxScroll = (settlementList.Count() * yspacing) - 264;
		}

		public void UiUpdate()
		{
			if (UIUpdateTimer < Find.TickManager.TicksAbs)
			{
				UIUpdateTimer = Find.TickManager.TicksAbs + FactionColonies.updateUiTimer;
				WindowUpdateFC();
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			
			//set text anchor and font
			GameFont fontBefore = Text.Font;
			TextAnchor anchorBefore = Text.Anchor;

			//Draw tabs
			DrawTabFaction(inRect);
			DrawTabColony(inRect);
			DrawTabReports(inRect);
			DrawTabEvent(inRect);

			//DrawColonySettlementCreationButton(inRect); //used for debugging

			if(tab == 0)
			{
				DrawFactionTopMenu(inRect);
				DrawFactionMiddleMenu(inRect);
				DrawFactionBottomMenu(inRect);
			}

			//Draw window based on tab
			if(tab == 1)
			{
				DrawColonySettlementCreationButton(inRect);
				DrawSettlementMenu(inRect);
				//DrawDebugButton(inRect);
				
				if (Event.current.type == EventType.ScrollWheel)
				{
					
					scrollWindow(Event.current.delta.y);
				}

			}

			//draw event select tab
			if (tab == 2)
			{

			}


			//first tests
			//DrawHeader(inRect);
			//DrawColonySettlementCreationButton(inRect);

			//Debug
			//DrawDebugButton(inRect);


			//Reset Text anchor and font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;
			

		}
		private void DrawHeader(Rect inRect)
		{
			Rect header = new Rect(0, 45, 150, 35);
			Text.Anchor = TextAnchor.UpperCenter;
			Text.Font = GameFont.Medium;
			Widgets.Label(header, "SettlementManager".Translate());


		}

		private void DrawColonySettlementCreationButton(Rect inRect)
		{
			Rect button = new Rect(InitialSize.x - 215, 40, 190, 20);
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Small;

			Faction gfaction = FactionColonies.getPlayerColonyFaction();
			if (gfaction != null)
			{

				if (Widgets.ButtonText(button, "CreateNewColony".Translate()))
				{
					Find.WindowStack.Add(new CreateColonyWindowFc());

					//Move player to world map
					Find.World.renderer.wantedMode = WorldRenderMode.Planet;

					Messages.Message("SelectTile".Translate(), MessageTypeDefOf.NegativeEvent);

				}
			} else //create new faction
			{
				if (Widgets.ButtonText(button, "Create New Faction"))
				{ 
					FactionColonies.createPlayerColonyFaction();
					faction.factionCreated = true;
					Find.WindowStack.Add(new FactionCustomizeWindowFc(faction));
					//Initial release - Autocreate faction
					if (Find.CurrentMap.Parent != null && Find.WorldObjects.WorldObjectAt<WorldSettlementFC>(Find.CurrentMap.Parent.Tile) != null)
					{
						Messages.Message( "SetAsFactionCapital".Translate(Find.WorldObjects.SettlementAt(Find.CurrentMap.Parent.Tile).Name), MessageTypeDefOf.NeutralEvent);
					}

				}
			}
		}


		private void DrawTabFaction(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;
			
			if(Widgets.ButtonTextSubtle(new Rect(0, 0, tabSize, 30), "Faction".Translate(), 0f, 8f, SoundDefOf.Mouseover_Category, new Vector2(-1f, -1f)))
			{
				tab = 0;
				faction.updateTotalProfit();
			}
		}
		private void DrawTabColony(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;

			if (Widgets.ButtonTextSubtle(new Rect(tabSize, 0, tabSize, 30), "Colonies".Translate(), 0f, 8f, SoundDefOf.Mouseover_Category, new Vector2(-1f, -1f)))
			{
				tab = 1;
				scroll = 0;
				maxScroll = (settlementList.Count() * yspacing)-264;
				foreach (SettlementFC settlement in faction.settlements)
				{
					settlement.updateProfitAndProduction();
				}
			}
		}
		private void DrawTabReports(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;

			if (Widgets.ButtonTextSubtle(new Rect(tabSize*2, 0, tabSize, 30), "Bills".Translate(), 0f, 8f, SoundDefOf.Mouseover_Category, new Vector2(-1f, -1f)))
			{
				Find.WindowStack.Add(new FCBillWindow());
				//Log.Message("Try open bills");
				
			}
		}
		private void DrawTabEvent(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;

			if (Widgets.ButtonTextSubtle(new Rect(tabSize*3, 0, tabSize, 30), "Events".Translate(), 0f, 8f, SoundDefOf.Mouseover_Category, new Vector2(-1f, -1f)))
			{
				//tab = 0;
				//Open Event window
				Find.WindowStack.Add(new FCEventWindow());
			}
		}

		private void DrawSettlementMenu(Rect inRect)
		{
			DrawSettlementHeader(inRect);
			DrawSettlementButtons(inRect);
		}
		private void DrawSettlementHeader(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(2, 32, 200, 40), "Settlements".Translate());
		}

		private void DrawSettlementButtons(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Tiny;

			//Reference
			//ist[0] = name;   //settlement name
			//list[1] = settlementLevel.ToString(); //settlement level
			//list[2] = settlementMilitaryLevel.ToString(); //settlement military level
			//list[3] = unrest.ToString(); //settlement unrest
			//list[4] = loyalty.ToString(); //settlement loyalty
			//list[5] = getTotalProfit().ToString(); //settlement profit
			//list[6] = mapLocation.ToString(); //settlement location
			//list[7] = ID

			List<string> headerList = new List<string> {"Settlement".Translate(), "Level".Translate(), "FreeWorkers".Translate(), "Unrest".Translate(), "Loyalty".Translate(), "Profit".Translate(), "Location", "ID" };
			int adjust2 = 0;

			
			Action method = delegate{ };

			
			for (int i = 0; i < headerList.Count() - 2; i++)  //-2 to exclude location and ID
			{
				int xspacingUpdated;
				GUIContent varString;
				switch (i)
				{
					case 0:
						xspacingUpdated = xspacing + headerSpacing;
						method = delegate { settlementList.Sort(FactionColonies.CompareSettlementName); };
						break;
					case 1:
						xspacingUpdated = xspacing - 10;
						method = delegate { settlementList.Sort(FactionColonies.CompareSettlementLevel); };
						break;
					case 2:
						xspacingUpdated = xspacing;
						method = delegate { settlementList.Sort(FactionColonies.CompareSettlementFreeWorkers); };
						break;
					case 3:
						xspacingUpdated = xspacing - 4;
						method = delegate { settlementList.Sort(FactionColonies.CompareSettlementUnrest); };
						break;
					case 4:
						xspacingUpdated = xspacing;
						method = delegate { settlementList.Sort(FactionColonies.CompareSettlementLoyalty); };
						break;
					case 5:
						xspacingUpdated = xspacing + 14;
						method = delegate { settlementList.Sort(FactionColonies.CompareSettlementProfit); };
						break;
					default:
						varString = new GUIContent("ERROR");
						xspacingUpdated = xspacing;
						break;
				}
				if (i == 0)
				{
					Widgets.Label(new Rect(2 + adjust2, 60, xspacingUpdated, 40), headerList[i]);
					if (Widgets.ButtonInvisible(new Rect(2 + adjust2, 60, xspacingUpdated, 40)))
					{
						method.Invoke();
					}
				}
				else
				{
					Widgets.Label(new Rect(2 + adjust2, 60, xspacingUpdated, 40), headerList[i]);
					if (Widgets.ButtonInvisible(new Rect(2 + adjust2, 60, xspacingUpdated, 40)))
					{
						method.Invoke();
					}
				}
				adjust2 += xspacingUpdated;
			}

			for (int i = 0; i < settlementList.Count(); i++) //browse through list.  settlementList[i] = a settlement
			{
				SettlementFC settlement = settlementList[i];
				if (i*yspacing + scroll >= 0 && i*yspacing + scroll <= 264) 
				{ 
					if (i % 2 == 0)
					{
						Widgets.DrawHighlight(new Rect(0, yoffset + i * yspacing + scroll, 312, 30));
					}
					int adjust = 0;
					for (int k = 0; k < 6; k++)  //Browse through settlement information    -2 to exclude location and ID
					{
						int xspacingUpdated;
						GUIContent varString;
						switch (k)
						{
							case 0:
								varString = new GUIContent(settlement.name);
								xspacingUpdated = xspacing + headerSpacing;
								break;
							case 1:
								varString = new GUIContent(settlement.settlementLevel.ToString());
								xspacingUpdated = xspacing - 10;
								break;
							case 2:
								varString = new GUIContent((settlement.workersUltraMax - settlement.getTotalWorkers()).ToString());
								xspacingUpdated = xspacing - 14;
								break;
							case 3:
								varString = new GUIContent(settlement.unrest.ToString(), TexLoad.iconUnrest);
								xspacingUpdated = xspacing + 4;
								break;
							case 4:
								varString = new GUIContent(settlement.loyalty.ToString(), TexLoad.iconLoyalty);
								xspacingUpdated = xspacing;
								break;
							case 5:
								varString = new GUIContent(settlement.totalProfit.ToString(), settlement.returnHighestResource().getIcon());
								xspacingUpdated = xspacing + 20;
								break;
							default:
								varString = new GUIContent("ERROR");
								xspacingUpdated = xspacing;
								break;
						}
						
						if (k == 0)
						{
							if(Widgets.ButtonText(new Rect(2 + adjust, yoffset + i * yspacing + scroll, xspacingUpdated, 30), ""))
							{  //When button of settlement name is pressed
								Find.WindowStack.Add(new SettlementWindowFc(settlement));

							}
							Widgets.Label(new Rect(2 + adjust, yoffset + i * yspacing + scroll, xspacingUpdated, 30), settlement.ShortName);
						}
						else
						{
							Widgets.Label(new Rect(2 + adjust, yoffset + i * yspacing + scroll, xspacingUpdated, 30), varString);

							//ist[0] = name;   //settlement name
							//list[1] = settlementLevel.ToString(); //settlement level
							//list[2] = settlementMilitaryLevel.ToString(); //settlement military level
							//list[3] = unrest.ToString(); //settlement unrest
							//list[4] = loyalty.ToString(); //settlement loyalty
							//list[5] = getTotalProfit().ToString(); //settlement profit
							//list[6] = mapLocation.ToString(); //settlement location
							//list[7] = ID
							//Widgets.Label(new Rect(headerSpacing + 2 + k * xspacing, yoffset + i * yspacing + scroll, xspacing, 30), varString);
						}
						adjust += xspacingUpdated;
					}
				}
			}
			//box outline
			Widgets.DrawBox(new Rect(0, 100, 312, 264));

		}

		private void DrawFactionName(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Medium;
			if (faction != null && faction.name != null)
				Widgets.Label(new Rect(7,32,200,40),faction.name);
			if(Widgets.ButtonImage(new Rect(210, 37, 20, 20), TexLoad.iconCustomize))
			{ //if click faction customize button
			  //Log.Message("Faction customize clicked");
				Faction fact = FactionColonies.getPlayerColonyFaction();
				if (fact != null)
					Find.WindowStack.Add(new FactionCustomizeWindowFc(faction));
				else
					Messages.Message("No faction created to customize", MessageTypeDefOf.RejectInput);
			}
		}
		private void DrawFactionTitle(Rect inRect)
		{
			Text.Anchor = TextAnchor.UpperCenter;
			Text.Font = GameFont.Tiny;
			Widgets.Label(new Rect(0, 60, 200, 20), faction.title);
		}

		private void DrawFactionIcon(Rect inRect)
		{
			Widgets.ButtonImage(new Rect(245, 40, 50, 50), faction.factionIcon);
			
		}

		private void DrawFactionTopMenu(Rect inRect)
		{
			Widgets.DrawMenuSection(new Rect(0, 32, 312, 65));
			DrawFactionName(inRect);
			DrawFactionTitle(inRect);
			DrawFactionIcon(inRect);
		}

		private void DrawFactionMiddleMenu(Rect inRect)
		{
			DrawFactionStats(inRect, statSize);
			DrawFactionButtons(buttonSize);
		}

		private void DrawFactionBottomMenu(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;
			Widgets.DrawMenuSection(new Rect(0, 270, 312, 90));
			Widgets.Label(new Rect(0, 270, 250, 30), "TotalProduction".Translate());
			DrawFactionResourceIcons(inRect, 0, 300, 20);

			DrawFactionEconomicStats(inRect);
		}

		private void DrawFactionStats(Rect inRect, int statSize)
		{
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Medium;
			for (int i = 0; i < stats.Count(); i++)
			{
				if (stats[i] == "happiness")
				{
					Widgets.DrawBox(new Rect(0, 105+ ((statSize + 15) * i), 125, statSize+10));
					Widgets.DrawHighlight(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize + 10));
					if(Widgets.ButtonImage(new Rect(5-2, 110 + ((statSize + 15) * i)-2, statSize+4, statSize+4), TexLoad.iconHappiness)) 
					{
						Find.WindowStack.Add(new DescWindowFc("FactionHappinessDesc".Translate(), "FactionHappiness".Translate()));
					};
					Widgets.Label(new Rect(50, 110 + ((statSize + 15) * i), 80, statSize), Convert.ToInt32(faction.averageHappiness) + "%");
				}
				if (stats[i] == "loyalty")
				{
					Widgets.DrawBox(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize + 10));
					Widgets.DrawHighlight(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize+10));
					if(Widgets.ButtonImage(new Rect(5-2, 110 + ((statSize + 15) * i)-2, statSize+4, statSize+4), TexLoad.iconLoyalty))
					{
						Find.WindowStack.Add(new DescWindowFc("FactionLoyaltyDesc".Translate(), "FactionLoyalty".Translate()));
					};
					Widgets.Label(new Rect(50, 110 + ((statSize+15) * i), 80, statSize), Convert.ToInt32(faction.averageLoyalty) + "%");
				}
				if (stats[i] == "unrest")
				{
					Widgets.DrawBox(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize + 10));
					Widgets.DrawHighlight(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize + 10));
					if(Widgets.ButtonImage(new Rect(5 - 2, 110 + ((statSize + 15) * i) - 2, statSize + 4, statSize + 4), TexLoad.iconUnrest))
					{
						Find.WindowStack.Add(new DescWindowFc("FactionUnrestDesc".Translate(), "FactionUnrest".Translate()));
					};
					Widgets.Label(new Rect(50, 110 + ((statSize + 15) * i), 80, statSize), Convert.ToInt32(faction.averageUnrest) + "%");
				}
				if (stats[i] == "prosperity")
				{
					Widgets.DrawBox(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize + 10));
					Widgets.DrawHighlight(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize + 10));
					if(Widgets.ButtonImage(new Rect(5 - 2, 110 + ((statSize + 15) * i) - 2, statSize + 4, statSize + 4), TexLoad.iconProsperity))
					{
						Find.WindowStack.Add(new DescWindowFc("FactionProsperityDesc".Translate(), "FactionProsperity".Translate()));
					};
					Widgets.Label(new Rect(50, 110 + ((statSize + 15) * i), 80, statSize), Convert.ToInt32(faction.averageProsperity) + "%");
				}
			}
			
		}

		private void DrawFactionButtons(int buttonSize) //Used to draw a list of buttons from the 'buttons' list
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;
			for (int i = 0; i < buttons.Count; i++)
			{
				if (Widgets.ButtonText(new Rect(140, 110 + ((buttonSize + 5) * i), 170, buttonSize), buttons[i]))
				{
					if (buttons[i] == "FCOverview".Translate())
					{ 
						Find.WindowStack.Add(new FCWindow_Overview());
					}
				

					if (buttons[i] == "Military".Translate())
					{
						if (FactionColonies.getPlayerColonyFaction() == null)
						{
							Messages.Message(new Message("NoFactionForMilitary".Translate(), MessageTypeDefOf.RejectInput));
						}
						else
						{
							Find.WindowStack.Add(new MilitaryCustomizationWindowFc());
						}
					}

					if (buttons[i] == "Actions".Translate())
					{
						List<FloatMenuOption> list = new List<FloatMenuOption>
						{
							new FloatMenuOption("TaxDeliveryMap".Translate(), delegate
							{
								List<FloatMenuOption> list2 = new List<FloatMenuOption> { new FloatMenuOption("SetMap".Translate(), delegate
								{
									List<FloatMenuOption> settlementList = new List<FloatMenuOption>();

									foreach (Map map in Find.Maps)
									{
										if (map.IsPlayerHome)
										{
											settlementList.Add(new FloatMenuOption(map.Parent.LabelCap, delegate
											{
												faction.taxMap = map;
												Find.LetterStack.ReceiveLetter("Map Set!", "The tax delivery map has been set to the player colony of " + map.Parent.LabelCap + ".\n All taxes and other goods will be delivered there", LetterDefOf.NeutralEvent);
											}
											));
										}
									}

									if (settlementList.Count == 0)
									{
										settlementList.Add(new FloatMenuOption("No valid settlements to use.", null));
									}

									FloatMenu floatMenu2 = new FloatMenu(settlementList);
									Find.WindowStack.Add(floatMenu2);
								}) };

								FloatMenu floatMenu = new FloatMenu(list2);
								Find.WindowStack.Add(floatMenu);
							}),

							new FloatMenuOption("SetCapital".Translate(), delegate
							{
								faction.setCapital();
							}),

							new FloatMenuOption("ActivateResearch".Translate(), delegate
							{
								faction.updateDailyResearch();
							}),

							new FloatMenuOption("ResearchLevel".Translate(), delegate
							{
								Messages.Message("CurrentResearchLevel".Translate(faction.techLevel.ToString(), faction.returnNextTechToLevel()), MessageTypeDefOf.NeutralEvent);
							}),

							new FloatMenuOption("FCOpenPatchNotes".Translate(), () => DebugActionsMisc.PatchNotesDisplayWindow())
                        };

                        if (faction.hasPolicy(FCPolicyDefOf.technocratic))
							list.Add(new FloatMenuOption("FCSendResearchItems".Translate(), delegate
							{
								if (Find.ColonistBar.GetColonistsInOrder().Count > 0) 
								{
									Pawn playerNegotiator = Find.ColonistBar.GetColonistsInOrder()[0];
									//Log.Message(playerNegotiator.Name + " Negotiator");

									FCTrader_Research trader = new FCTrader_Research();
									
									Find.WindowStack.Add(new Dialog_Trade(playerNegotiator, trader));
								} 
								else
								{
									Log.Error("Couldn't find any colonists to trade with");
								}
							}));

						if (faction.hasPolicy(FCPolicyDefOf.feudal))
							list.Add(new FloatMenuOption("FCRequestMercenary".Translate(), delegate
							{
								if (faction.traitFeudalBoolCanUseMercenary)
								{

									faction.traitFeudalBoolCanUseMercenary = false;
									faction.traitFeudalTickLastUsedMercenary = Find.TickManager.TicksGame;

                                    PawnGenerationRequest request = FCPawnGenerator.WorkerOrMilitaryRequest();
									request.ColonistRelationChanceFactor = 20f;
									Pawn pawn = PawnGenerator.GeneratePawn(request);

                                    IncidentParms parms = new IncidentParms
                                    {
                                        target = Find.CurrentMap,
                                        faction = FactionColonies.getPlayerColonyFaction(),
                                        points = 999,
                                        raidArrivalModeForQuickMilitaryAid = true,
                                        raidNeverFleeIndividual = true,
                                        raidForceOneIncap = true,
                                        raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop,
                                        raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly
                                    };
                                    parms.raidArrivalModeForQuickMilitaryAid = true;
									PawnsArrivalModeWorker_EdgeWalkIn worker = new PawnsArrivalModeWorker_EdgeWalkIn();
									worker.TryResolveRaidSpawnCenter(parms);
									worker.Arrive(new List<Pawn> { pawn }, parms);

									Find.LetterStack.ReceiveLetter("FCMercenaryJoined".Translate(), "FCMercenaryJoinedText".Translate(pawn.NameFullColored), LetterDefOf.PositiveEvent, new LookTargets(pawn));
									pawn.SetFaction(Faction.OfPlayer);
								}
								else
								{
									Messages.Message("FCActionMercenaryOnCooldown".Translate(((faction.traitFeudalTickLastUsedMercenary + GenDate.TicksPerSeason) - Find.TickManager.TicksGame).ToTimeString()), MessageTypeDefOf.RejectInput);
								}
							}));


						FloatMenu menu = new FloatMenu(list);
						Find.WindowStack.Add(menu);
					}
				}
			}
		}

		private void DrawFactionResourceIcons(Rect inRect, int x, int y, int resourceSize) //Used to draw a list of resources from the faction
		{
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			int k;
			int j;
			float resourcesPerRow = 7;
			int ySpacing = 30;

			foreach (ResourceType resourceType in ResourceUtils.resourceTypes)
			{
				ResourceFC resource = faction.returnResource(resourceType);
				k = (int)Math.Floor((int) resourceType / resourcesPerRow);
				j = (int)((int) resourceType % resourcesPerRow);
				if(Widgets.ButtonImage(new Rect(5 + x + (j * (resourceSize+5)), y-5 + ySpacing*k, resourceSize, 
					resourceSize), resource.getIcon()))
				{
					Find.WindowStack.Add(new DescWindowFc("TotalFactionProduction".Translate() + ": " + 
					                                      resource.name, 
						char.ToUpper(resource.name[0]) + 
						resource.name.Substring(1)));
				}
				Widgets.Label(new Rect(5 + x + j * (resourceSize + 5), y+resourceSize-10 + ySpacing * k, 
					resourceSize, resourceSize), resource.amount.ToString());
			}
		}

		private void DrawFactionEconomicStats(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Tiny;
			Widgets.Label(new Rect(195, 270, 115, 30), "EstimatedProfit".Translate());
			Widgets.Label(new Rect(195, 300, 115, 20), Convert.ToInt32(faction.profit) + " " + "Silver".Translate().ToLower());

			Widgets.Label(new Rect(195, 315, 115, 30), "TimeTillTax".Translate() + ":");
			Widgets.Label(new Rect(195, 340, 115, 20), Math.Max(0, faction.taxTimeDue-Find.TickManager.TicksGame).ToTimeString());
		}
	
		private void scrollWindow(float num)
		{
			if (scroll - num * 5 < -1 * maxScroll)
			{
				scroll = -1 * maxScroll;
			} else if (scroll - num*5 > 0){
				scroll = 0;
			} else
			{
				scroll -= (int)Event.current.delta.y * 5;
			}
			Event.current.Use();
		}
	
	}

}
