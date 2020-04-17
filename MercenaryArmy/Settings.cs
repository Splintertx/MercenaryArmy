using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ModLib;
using ModLib.Attributes;
using System.Xml.Serialization;

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
                if (_instance == null)
                {
                    _instance = FileDatabase.Get<Settings>(SettingsInstanceID);
                    if (_instance == null)
                    {
                        _instance = new Settings();
                        SettingsDatabase.SaveSettings(_instance);
                    }
                }

                return _instance;
            }
        }

        [XmlElement]
        public override string ID { get; set; } = SettingsInstanceID;

        [XmlElement]
        [SettingProperty("Create Player Kingdom for Independent Army Cheat", "(Default false) Automatically create a player kingdom when attempting to form an independent army. THIS IS A CHEAT!")]
        public bool CreatePlayerKingdom { get; set; } = false;
    }
}
