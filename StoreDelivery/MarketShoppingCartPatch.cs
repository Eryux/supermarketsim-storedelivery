// Copyright(c) 2024 - C.Nicolas <contact@bark.tf>
// github.com/eryux

using HarmonyLib;
using MyBox;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace StoreDelivery
{
    class MarketShoppingCartPatch
    {
        public static Plugin _plugin;

        [HarmonyPatch(typeof(MarketShoppingCart), nameof(MarketShoppingCart.UpdateTotalPrice))]
        [HarmonyPostfix]
        static void UpdateTotalPricePostfix(MarketShoppingCart __instance)
        {
            int totalItem = 0;

            var cartData = typeof(MarketShoppingCart).GetField("m_CartData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as MarketShoppingCart.CartData;

            for (int i = 0; i < cartData.ProductInCarts.Count; ++i)
            {
                totalItem += cartData.ProductInCarts[i].FirstItemCount;
            }

            float grandTotal = (float)(typeof(MarketShoppingCart).GetField("m_OrderTotalPrice", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance)) + __instance.CurrentShippingCost;
            
            if (_plugin != null && _plugin.Cfg.ConfigFeesEnabled.Value && _plugin.StoreDeliveryCart != null)
            {
                float totalCost = _plugin.Cfg.ConfigPerBoxesCost.Value * totalItem;
                _plugin.StoreDeliveryCart.UpdateCost(totalCost);
                grandTotal += totalCost;
            }

            bool hasMoney = MyBox.Singleton<MoneyManager>.Instance.HasMoney(grandTotal);
            typeof(MarketShoppingCart).GetMethod("UpdateUI", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { hasMoney });
        }


        [HarmonyPatch(typeof(MarketShoppingCart), "UpdateUI")]
        [HarmonyPostfix]
        static void UpdateUIPostfix(MarketShoppingCart __instance, bool hasMoney)
        {
            if (_plugin != null && _plugin.Cfg.ConfigFeesEnabled.Value && _plugin.StoreDeliveryCart != null && _plugin.StoreDeliveryCart.StorageEnabled)
            {
                TMP_Text[] totalPriceTexts = (TMP_Text[])typeof(MarketShoppingCart).GetField("m_TotalPriceTexts", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

                float grandTotal = (float)(typeof(MarketShoppingCart).GetField("m_OrderTotalPrice", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance))
                    + __instance.CurrentShippingCost + _plugin.StoreDeliveryCart.StorageCost;

                foreach (TMP_Text tmp_Text2 in totalPriceTexts)
                {
                    tmp_Text2.text = Extensions.ToMoneyText(grandTotal, tmp_Text2.fontSize);
                }

                typeof(MarketShoppingCart).GetMethod("UpdateBalance", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { 0f, MoneyManager.TransitionType.NONE });
            }
        }


        [HarmonyPatch(typeof(MarketShoppingCart), "UpdateBalance")]
        [HarmonyPostfix]
        static void UpdateBalancePostfix(MarketShoppingCart __instance)
        {
            if (_plugin != null && _plugin.Cfg.ConfigFeesEnabled.Value && _plugin.StoreDeliveryCart != null && _plugin.StoreDeliveryCart.StorageEnabled)
            {
                var m_OrderTotalPrice = (float)typeof(MarketShoppingCart).GetField("m_OrderTotalPrice", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                var m_BalanceText = (TMP_Text)typeof(MarketShoppingCart).GetField("m_BalanceText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                var m_RemainingMoneyText = (TMP_Text)typeof(MarketShoppingCart).GetField("m_RemainingMoneyText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                var m_EnoughMoneyTextColor = (Color)typeof(MarketShoppingCart).GetField("m_EnoughMoneyTextColor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                var m_NotEnoughMoneyTextColor = (Color)typeof(MarketShoppingCart).GetField("m_NotEnoughMoneyTextColor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                var m_PurchaseButton = (Button)typeof(MarketShoppingCart).GetField("m_PurchaseButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                var m_CartData = (MarketShoppingCart.CartData)typeof(MarketShoppingCart).GetField("m_CartData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                var m_MarketClosed = (bool)typeof(MarketShoppingCart).GetField("m_MarketClosed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                var m_TotalPriceTexts = (TMP_Text[])typeof(MarketShoppingCart).GetField("m_TotalPriceTexts", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

                // Taken from MarketShoppingCart.UpdateBalance
                // Added _plugin.StoreDeliveryCart.StorageCost to total order cost
                bool flag = Singleton<MoneyManager>.Instance.HasMoney(m_OrderTotalPrice + __instance.CurrentShippingCost + _plugin.StoreDeliveryCart.StorageCost);
                m_BalanceText.text = Singleton<MoneyManager>.Instance.Money.ToMoneyText(m_BalanceText.fontSize);
                m_RemainingMoneyText.color = (flag ? m_EnoughMoneyTextColor : m_NotEnoughMoneyTextColor);
                m_RemainingMoneyText.text = (Singleton<MoneyManager>.Instance.Money - (m_OrderTotalPrice + __instance.CurrentShippingCost + _plugin.StoreDeliveryCart.StorageCost)).ToMoneyText(m_RemainingMoneyText.fontSize);
                m_PurchaseButton.interactable = (flag && (m_CartData.ProductInCarts.Count > 0 || m_CartData.FurnituresInCarts.Count > 0) && !m_MarketClosed);
                TMP_Text[] totalPriceTexts = m_TotalPriceTexts;
                for (int i = 0; i < totalPriceTexts.Length; i++)
                {
                    totalPriceTexts[i].color = (flag ? m_EnoughMoneyTextColor : m_NotEnoughMoneyTextColor);
                }
            }
        }

        [HarmonyPatch(typeof(MarketShoppingCart), nameof(MarketShoppingCart.Purchase))]
        [HarmonyPrefix]
        static void PurchasePrefix(MarketShoppingCart __instance)
        {
            if (_plugin != null && _plugin.Cfg.ConfigFeesEnabled.Value && _plugin.StoreDeliveryCart != null && _plugin.StoreDeliveryCart.StorageEnabled)
            {
                Singleton<MoneyManager>.Instance.MoneyTransition(_plugin.StoreDeliveryCart.StorageCost * -1f, MoneyManager.TransitionType.SUPPLY_COSTS);
            }
        }
    }
}
