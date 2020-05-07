using System;
using System.Windows.Forms;

using TaleWorlds.MountAndBlade;

using HarmonyLib;

namespace MercenaryArmy
{
    class SubModule : MBSubModuleBase
    {
        public static readonly string ModuleFolderName = "MercenaryArmy";

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                var harmony = new Harmony("mod.bannerlord.splintert");
                harmony.PatchAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Initializing MercenaryArmy:\n\n{ex.Message}");
            }
        }
    }
}
