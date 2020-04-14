using System;
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

namespace MercenaryArmy
{
    [HarmonyPatch(typeof(MapVM), "CanGatherArmyWithReason")]
    public class MapVMMod
    {
        static TextObject err1;
        static TextObject err2;

        static void Postfix(ref bool __result, ref string reasonText)
        {
            if (err1 == null || err2 == null)
            {
                err1 = GameTexts.FindText("str_need_to_be_a_part_of_kingdom", (string)null);
                err2 = GameTexts.FindText("str_mercenary_cannot_manage_army", (string)null);
            }
            if (__result == false && new[] { err1.ToString(), err2.ToString() }.Contains(reasonText))
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
}
