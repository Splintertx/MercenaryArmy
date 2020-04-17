using HarmonyLib;
using System;
using System.Windows.Forms;
using TaleWorlds.MountAndBlade;

using ModLib;
using ModLib.Debugging;

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
                FileDatabase.Initialise(ModuleFolderName);
                SettingsDatabase.RegisterSettings(Settings.Instance);

                var harmony = new Harmony("mod.bannerlord.splintert");
                harmony.PatchAll();
            }
            catch (Exception ex)
            {
                ModDebug.ShowError($"Error Initializing MercenaryArmy:\n\n{ex.ToStringFull()}");
            }
        }
    }
}
