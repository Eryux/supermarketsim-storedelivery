// Copyright(c) 2024 - C.Nicolas <contact@bark.tf>
// github.com/eryux

using HarmonyLib;

namespace StoreDelivery
{
    [HarmonyPatch(typeof(DeliveryManager), nameof(DeliveryManager.Delivery))]
    class DeliveryManagerPatch
    {
        public static Plugin _plugin;


        static void Prefix(ref CartData cartData)
        {
            if (_plugin != null)
            {
                if (cartData != null)
                {
                    for (int i = 0; i < cartData.ProductInCarts.Count; ++i)
                    {
                        ItemQuantity productQuantity = cartData.ProductInCarts[i];
                        _plugin.AddProductDeliveryStock(productQuantity.FirstItemID, productQuantity.FirstItemCount);
                    }
                }
            }
        }
    }
}
