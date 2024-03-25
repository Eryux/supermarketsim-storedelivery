// Copyright(c) 2024 - C.Nicolas <contact@bark.tf>
// github.com/eryux

using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StoreDelivery
{
    [BepInPlugin("tf.bark.sms.StoreDelivery", "StoreDelivery", "1.0.2")]
    [BepInProcess("Supermarket Simulator.exe")]
    public class Plugin : BaseUnityPlugin
    {
        Harmony _patches;


        bool _mainSceneLoaded = false;

        AssetBundle _assetBundle;


        Dictionary<int, int> _productDeliveryStocks = new Dictionary<int, int>();

        StoreDeliveryCart _storeDeliveryCart;

        public StoreDeliveryCart StoreDeliveryCart { get { return _storeDeliveryCart; } }


        public Config Cfg { get; internal set; }


        private void Awake()
        {
            // Load configuration ---------------

            Cfg = new Config(Config);
            Logger.LogInfo("Configuration loaded");

            if (!Cfg.ConfigEnabled.Value)
            {
                Destroy(this);
                return;
            }

            // Apply patches --------------------

            Logger.LogInfo("Apply patches...");
            DeliveryManagerPatch._plugin = this;
            MarketShoppingCartPatch._plugin = this;
            _patches = new Harmony("tf.bark.sms.StoreDelivery.patches");
            _patches.PatchAll(typeof(DeliveryManagerPatch));
            _patches.PatchAll(typeof(MarketShoppingCartPatch));

            // Load assets ----------------------

            Logger.LogInfo($"Loading assets...");

            foreach (string x in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                Logger.LogInfo(x);
            }

            try
            {
                byte[] buff = null;

                using (Stream reader = Assembly.GetExecutingAssembly().GetManifestResourceStream("StoreDelivery.assets.storedelivery_ui"))
                {
                    buff = new byte[reader.Length];
                    reader.Read(buff, 0, buff.Length);
                }

                _assetBundle = AssetBundle.LoadFromMemory(buff);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to load assets!");
                Logger.LogError(ex.ToString());
                Destroy(this);
                return;
            }

            // ----------------------------------

            Logger.LogInfo("Mod is loaded!");
        }

        private void OnDestroy()
        {
            if (_patches != null)
            {
                _patches.UnpatchSelf();
            }

            if (_storeDeliveryCart != null)
            {
                Destroy(_storeDeliveryCart.gameObject);
            }

            Logger.LogInfo("Mod is unloaded.");
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _mainSceneLoaded = (scene.buildIndex == 1);

            if (_mainSceneLoaded)
            {
                Initialize();
            }
        }

        // --------------------------------------

        void Initialize()
        {
            GameObject storeDeliveryCartObject = new GameObject("storeDeliveryPlugin_CartManager");
            storeDeliveryCartObject.transform.position = Vector3.zero;
            storeDeliveryCartObject.transform.rotation = Quaternion.identity;

            _storeDeliveryCart = storeDeliveryCartObject.AddComponent<StoreDeliveryCart>();
            _storeDeliveryCart.Plugin = this;
            _storeDeliveryCart.Assets = _assetBundle;
        }

        public void AddProductDeliveryStock(int productID, int quantity)
        {
            if (_storeDeliveryCart != null && _storeDeliveryCart.StorageEnabled)
            {
                if (_productDeliveryStocks.ContainsKey(productID))
                {
                    _productDeliveryStocks[productID] += quantity;
                }
                else
                {
                    _productDeliveryStocks[productID] = quantity;
                }
            }
        }

        public void StockBoxInRacks()
        {
            if (_storeDeliveryCart == null || !_storeDeliveryCart.StorageEnabled)
            {
                return;
            }

            Logger.LogDebug("Add boxes to stockage racks");

            DeliveryManager deliveryManager = MyBox.Singleton<DeliveryManager>.Instance;
            if (deliveryManager != null)
            {
                List<Box> boxes = new List<Box>();

                for (int i = 0; i < deliveryManager.transform.childCount; ++i)
                {
                    Box box = deliveryManager.transform.GetChild(i).GetComponent<Box>();

                    if (box != null)
                    {
                        Logger.LogDebug("Find box with product " + box.Product.ID);

                        if (_productDeliveryStocks.ContainsKey(box.Product.ID))
                        {
                            Logger.LogDebug("Stock " + _productDeliveryStocks[box.Product.ID]);
                        }

                        if (box != null && _productDeliveryStocks.ContainsKey(box.Product.ID) && _productDeliveryStocks[box.Product.ID] > 0)
                        {
                            boxes.Add(box);
                            _productDeliveryStocks[box.Product.ID] -= 1;
                        }
                    }
                }

                RackManager rackManager = MyBox.Singleton<RackManager>.Instance;
                if (rackManager != null)
                {
                    for (int i = 0; i < boxes.Count; ++i)
                    {
                        Box box = boxes[i];

                        Logger.LogDebug("Checking for box " + box.BoxID + " with product " + box.Product.ID);

                        RackSlot rackSlot = RackTool.GetRackSlotFor(box, Cfg.ConfigUseEmptyRackSlot.Value, Cfg.ConfigUseEmptyRackWithLabel.Value);

                        if (rackSlot != null)
                        {
                            Logger.LogDebug("Rack found for " + box.BoxID);
                            RackTool.PlaceBoxInRack(rackSlot, box);
                        }
                    }
                }
            }
        }
    }
}
