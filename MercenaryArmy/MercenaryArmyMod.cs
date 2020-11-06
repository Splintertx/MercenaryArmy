using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.GameMenus;

namespace MercenaryArmy
{
    [HarmonyPatch(typeof(MapVM), "CanGatherArmyWithReason")]
    public class MapVMMod
    {
        static List<string> errs = new List<string>();

        static void Postfix(ref bool __result, ref string reasonText)
        {
            if (!errs.Any())
            {

                //No need for cheat anymore
                //if (Settings.Instance.CreatePlayerKingdom)
                errs.Add(GameTexts.FindText("str_need_to_be_a_part_of_kingdom", (string)null).ToString());
                errs.Add(GameTexts.FindText("str_mercenary_cannot_manage_army", (string)null).ToString());
            }

            if (__result == false && errs.Contains(reasonText))
            {
                __result = true;
                reasonText = String.Empty;
            }
        }
    }

    [HarmonyPatch(typeof(ArmyManagementVM), MethodType.Constructor, new Type[] { typeof(Action) })]
    public class ArmyManagementVMMod
    {
        static void Postfix(ArmyManagementVM __instance)
        {
            if (!Hero.MainHero.MapFaction.IsKingdomFaction || Clan.PlayerClan.IsUnderMercenaryService)
            {
                for (int x = 0; x < __instance.PartyList.Count; x++)
                {
                    if (__instance.PartyList[x].Clan.Id != MobileParty.MainParty.LeaderHero.Clan.Id)
                    {
                        __instance.PartyList.RemoveAt(x);
                        x--;
                    }
                }
            }
        }
    }


    /// <summary>
    /// Initialise hourly tick for armies not in kingdom from player
    /// </summary>
    [HarmonyPatch(typeof(Campaign), "InitializeCampaignObjectsOnAfterLoad")]
    public class CampaignMod
    {
        static void Postfix(Campaign __instance)
        {
            if (__instance.MainParty.Army != null)
            {
                Traverse traverse = Traverse.Create((object)__instance.MainParty.Army).Method("OnAfterLoad", (object[])null);
                if (traverse.MethodExists())
                {
                    traverse.GetValue();
                }
            }
        }
    }

    [HarmonyPatch(typeof(MobileParty), "ConsiderMapEventsAndSiegesInternal")]
    public class MobilePartyMod
    {
        static bool Prefix(MobileParty __instance, IFaction factionToConsiderAgainst)
        {
            //we exit and go back to normal if army is not player's army
            if (__instance.Army == null || __instance.Army.LeaderParty == null || !__instance.Army.LeaderParty.IsMainParty)
                return true;
            if (__instance.Army != null && __instance.Army.Kingdom != null && __instance.Army.Kingdom != __instance.MapFaction)
                __instance.Army = (Army)null;
            if (__instance.CurrentSettlement != null)
            {
                IFaction mapFaction = __instance.CurrentSettlement.MapFaction;
                if (mapFaction != null && mapFaction.IsAtWarWith(__instance.MapFaction) && __instance.IsRaiding || __instance.IsMainParty && (PlayerEncounter.Current.ForceRaid || PlayerEncounter.Current.ForceSupplies || PlayerEncounter.Current.ForceVolunteers))
                    return false;
            }
            if (__instance.Party.MapEventSide != null)
            {
                IFaction mapFaction1 = __instance.Party.MapEventSide.OtherSide.MapFaction;
                IFaction mapFaction2 = __instance.Party.MapEventSide.MapFaction;
                BattleSideEnum side = PlayerEncounter.Battle != null ? PlayerEncounter.Battle.PlayerSide : BattleSideEnum.None;
                if (mapFaction1 == null || !mapFaction1.IsAtWarWith(__instance.MapFaction) && mapFaction1 == factionToConsiderAgainst)
                {
                    if (__instance.Party == PartyBase.MainParty && PlayerEncounter.Current != null && __instance.Party.SiegeEvent != null)
                        PlayerEncounter.Current.SetPlayerSiegeInterruptedByPeace();
                    __instance.Party.MapEventSide = (MapEventSide)null;
                }
                else if (mapFaction2 == null || mapFaction2.IsAtWarWith(__instance.MapFaction) && mapFaction1 == factionToConsiderAgainst)
                    __instance.Party.MapEventSide = (MapEventSide)null;
                if (__instance.Party == PartyBase.MainParty && PlayerEncounter.Current != null && (PlayerEncounter.Battle != null && PlayerEncounter.Battle.PartiesOnSide(GetOtherSide(side)).Any<PartyBase>((Func<PartyBase, bool>)(x => x.MapFaction == factionToConsiderAgainst))) && !PlayerEncounter.EncounteredParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
                    PlayerEncounter.Finish(true);
            }
            if (__instance.BesiegerCamp != null)
            {
                IFaction mapFaction1 = __instance.BesiegerCamp.SiegeEvent.BesiegedSettlement.MapFaction;
                IFaction mapFaction2 = __instance.BesiegerCamp.BesiegerParty?.MapFaction;
                if (mapFaction1 == null || !mapFaction1.IsAtWarWith(__instance.MapFaction) && mapFaction1 == factionToConsiderAgainst)
                    __instance.BesiegerCamp = (BesiegerCamp)null;
                else if (mapFaction2 == null || mapFaction2.IsAtWarWith(__instance.MapFaction) && mapFaction1 == factionToConsiderAgainst)
                    __instance.BesiegerCamp = (BesiegerCamp)null;
            }
            if (__instance.CurrentSettlement == null)
                return false;
            IFaction mapFaction3 = __instance.CurrentSettlement.MapFaction;
            if (mapFaction3 == null || mapFaction3 != factionToConsiderAgainst || !mapFaction3.IsAtWarWith(__instance.MapFaction))
                return false;
            if (__instance.IsMainParty && !__instance.IsRaiding)
            {
                if (GameStateManager.Current.ActiveState.IsMission || !__instance.CurrentSettlement.IsFortification)
                    return false;
                GameMenu.SwitchToMenu("fortification_crime_rating");
            }
            else
                LeaveSettlementAction.ApplyForParty(__instance);
            return false;
        }

        private static BattleSideEnum GetOtherSide(BattleSideEnum side)
        {
            return side != BattleSideEnum.Attacker ? BattleSideEnum.Attacker : BattleSideEnum.Defender;
        }
    }

    [HarmonyPatch(typeof(ArmyManagementVM), "ExecuteDone")]
    public class ExecuteDoneMod
    {
        static bool Prefix(ArmyManagementVM __instance, MBBindingList<ArmyManagementItemVM> ____partiesToRemove, Action ____onClose)
        {
            // Only enable if player is independent
            // THIS IS A PORT OF ArmyManagementVM.ExecuteDone!
            if (!Hero.MainHero.MapFaction.IsKingdomFaction)
            {
                int num = __instance.PartiesInCart.Sum<ArmyManagementItemVM>((Func<ArmyManagementItemVM, int>)(P => P.Cost));
                bool flag1 = (double)num <= (double)Hero.MainHero.Clan.Influence;
                if (flag1 && __instance.NewCohesion > __instance.Cohesion)
                {
                    if (MobileParty.MainParty.Army == null)
                        return false;
                    ArmyManagementCalculationModel calculationModel = Campaign.Current.Models.ArmyManagementCalculationModel;
                    int num1 = __instance.NewCohesion - __instance.Cohesion;
                    Army army = MobileParty.MainParty.Army;
                    double num2 = (double)num1;
                    int totalInfluenceCost = calculationModel.CalculateTotalInfluenceCost(army, (float)num2);
                    MobileParty.MainParty.Army.BoostCohesionWithInfluence((float)num1, totalInfluenceCost);
                }
                if (__instance.PartiesInCart.Count > 1 & flag1/* && MobileParty.MainParty.MapFaction.IsKingdomFaction*/)
                {
                    if (MobileParty.MainParty.Army == null)
                    {
                        Army army = new Army((Kingdom)null, Hero.MainHero.PartyBelongedTo, Army.ArmyTypes.Patrolling, Hero.MainHero.HomeSettlement, (Hero)null)
                        {
                            AIBehavior = Army.AIBehaviorFlags.Gathering
                        };
                        army.Gather();
                        Traverse traverse = Traverse.Create((object)CampaignEventDispatcher.Instance).Method("OnArmyCreated", new Type[1]
                        {
                                    typeof (Army)
                        }, (object[])null);
                        if (traverse.MethodExists())
                        {
                            traverse.GetValue((object)army);
                        }
                        if (army.LeaderParty.LeaderHero == Hero.MainHero && (Game.Current.GameStateManager.GameStates.Single<GameState>((Func<GameState, bool>)(S => S is MapState)) is MapState mapState))
                        {
                            mapState.OnArmyCreated(MobileParty.MainParty);
                        }
                    }
                    foreach (ArmyManagementItemVM managementItemVm in (Collection<ArmyManagementItemVM>)__instance.PartiesInCart)
                    {
                        if (managementItemVm.Party != MobileParty.MainParty)
                        {
                            managementItemVm.Party.Army = MobileParty.MainParty.Army;
                            SetPartyAiAction.GetActionForEscortingParty(managementItemVm.Party, MobileParty.MainParty);
                            managementItemVm.Party.IsJoiningArmy = true;
                        }
                    }
                    Hero.MainHero.Clan.Influence -= (float)num;
                }
                if (____partiesToRemove.Count > 0)
                {
                    bool flag2 = false;
                    foreach (ArmyManagementItemVM managementItemVm in (Collection<ArmyManagementItemVM>)____partiesToRemove)
                    {
                        if (managementItemVm.Party == MobileParty.MainParty)
                        {
                            managementItemVm.Party.Army = (Army)null;
                            flag2 = true;
                        }
                    }
                    if (!flag2)
                    {
                        foreach (ArmyManagementItemVM managementItemVm in (Collection<ArmyManagementItemVM>)____partiesToRemove)
                        {
                            Army army = MobileParty.MainParty.Army;
                            if ((army != null ? (army.Parties.Contains(managementItemVm.Party) ? 1 : 0) : 0) != 0)
                                managementItemVm.Party.Army = (Army)null;
                        }
                    }
                    ____partiesToRemove.Clear();
                }
                if (flag1)
                    ____onClose();
                else
                    InformationManager.AddQuickInformation(new TextObject("{=Xmw93W6a}Not Enough Influence", (Dictionary<string, TextObject>)null), 0, (BasicCharacterObject)null, "");

                return false;
            }

            return true;
        }
    }
}
