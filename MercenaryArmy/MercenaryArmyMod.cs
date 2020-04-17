using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.Actions;

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
                // Only enable the army button for independents if the hack is enabled
                if (Settings.Instance.CreatePlayerKingdom)
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

    [HarmonyPatch(typeof(ArmyManagementVM), "ExecuteDone")]
    public class ExecuteDoneMod
    {
        static bool Prefix(ArmyManagementVM __instance, MBBindingList<ArmyManagementItemVM> ____partiesToRemove, Action ____onClose)
        {
            // Only enable this hack if it is enabled and player is independent
            // THIS IS A PORT OF ArmyManagementVM.ExecuteDone!
            if (!Hero.MainHero.MapFaction.IsKingdomFaction && Settings.Instance.CreatePlayerKingdom)
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
                        if ((MobileParty.MainParty.MapFaction as Kingdom) is null)
                            CampaignCheats.CreatePlayerKingdom(new List<string>());
                        
                        ((Kingdom)MobileParty.MainParty.MapFaction).CreateArmy(Hero.MainHero, (IMapPoint)Hero.MainHero.HomeSettlement, Army.ArmyTypes.Patrolling);
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
