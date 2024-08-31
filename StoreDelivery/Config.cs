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

        public ConfigEntry<bool> ConfigUseEmptyRackSlot { get; internal set; }

        public ConfigEntry<bool> ConfigUseEmptyRackWithLabel { get; internal set; }

        public ConfigEntry<bool> ConfigOrganizeRack { get; internal set; }


        public Config(ConfigFile cfg)
        {
            ConfigEnabled = cfg.Bind("StoreDelivery", "enabled", true, "Enable StoreDelivery Plugin");
            ConfigFeesEnabled = cfg.Bind("StoreDelivery", "fees_enables", true, "Apply fees when delivery is stored");
            ConfigPerBoxesCost = cfg.Bind("StoreDelivery", "fees_per_boxes", 1f, "Fees added for each product box in your delivery");
            ConfigUseEmptyRackSlot = cfg.Bind("StoreDelivery", "use_empty_rack", false, "Allow the mod to place box on empty rack with no label");
            ConfigUseEmptyRackWithLabel = cfg.Bind("StoreDelivery", "use_empty_rack_w_label", false, "Allow the mod to place box on empty rack even if its label doesn't match with the product");
            ConfigOrganizeRack = cfg.Bind("StoreDelivery", "organize_rack", true, "Sort boxes on rack to have boxes with less products on top when boxes are added after a purchase.");
        }
    }
}
