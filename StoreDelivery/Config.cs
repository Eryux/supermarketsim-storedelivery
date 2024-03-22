// Copyright(c) 2024 - C.Nicolas <contact@bark.tf>
// github.com/eryux

using BepInEx.Configuration;


namespace StoreDelivery
{
    public class Config
    {

        public ConfigEntry<bool> ConfigEnabled { get; internal set; }

        public ConfigEntry<bool> ConfigFeesEnabled { get; internal set; }

        public ConfigEntry<float> ConfigPerBoxesCost { get; internal set; }


        public Config(ConfigFile cfg)
        {
            ConfigEnabled = cfg.Bind("StoreDelivery", "enabled", true, "Enable StoreDelivery Plugin");
            ConfigFeesEnabled = cfg.Bind("StoreDelivery", "fees_enables", true, "Apply fees when delivery is stored");
            ConfigPerBoxesCost = cfg.Bind("StoreDelivery", "fees_per_boxes", 3f, "Fees added for each product box in your delivery");
        }
    }
}
