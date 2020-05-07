using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using ModLib.Definitions;
using ModLib.Definitions.Attributes;

namespace MercenaryArmy
{
    public class Settings : SettingsBase
    {
        public override string ModName => "Mercenary Army";
        public override string ModuleFolderName => SubModule.ModuleFolderName;
        public const string SettingsInstanceID = "MercenaryArmySettings";
        public static Settings _instance = null;

        public static Settings Instance
        {
            get
            {
                return (Settings)SettingsDatabase.GetSettings<Settings>();
            }
        }

        [XmlElement]
        public override string ID { get; set; } = SettingsInstanceID;

        [XmlElement]
        [SettingProperty("Create Player Kingdom for Independent Army Cheat", "(Default false) Automatically create a player kingdom when attempting to form an independent army. THIS IS A CHEAT!")]
        public bool CreatePlayerKingdom { get; set; } = false;
    }
}
