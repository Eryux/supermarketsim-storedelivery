// Copyright(c) 2024 - C.Nicolas <contact@bark.tf>
// github.com/eryux

using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Linq;


namespace StoreDelivery
{
    public class StoreDeliveryCart : MonoBehaviour
    {
        public Plugin Plugin { get; set; }

        public AssetBundle Assets { get; set; }


        public float StorageCost { get; internal set; } = 0f;


        MarketShoppingCart _shoppingCart;

        RectTransform _cartToggleContainerTransform;

        Toggle _cartToggle;

        RectTransform _cartStorageCostLabelTransform;

        TextMeshProUGUI _cartStorageCostValueText;

        RectTransform _divideTransform;

        RectTransform _totalLabelTransform;

        RectTransform _totalPriceTransform;

        Vector2 _dividePositionDefault;

        Vector2 _totalLabelPositionDefault;

        Vector2 _totalPricePositionDefault;

        TMP_FontAsset _font;


        bool _enableStorage = true;

        public bool StorageEnabled { get { return _enableStorage; } }


        bool _initialized = false;


        void Start()
        {
            if (SceneManager.GetActiveScene().buildIndex == 1 && Plugin != null)
            {
                Initialize();
            }
        }


        void Initialize()
        {
            if (!_initialized)
            {
                _font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(x => x.name == "UptownBoy SDF");

                CreateAndAddCartUI();

                _initialized = true;
            }

            ToggleStorage(_enableStorage, false);
        }

        void OnDestroy()
        {
            if (_initialized)
            {
                HideStorageCost();
                Destroy(_cartToggleContainerTransform.gameObject);
                Destroy(_cartStorageCostLabelTransform.gameObject);
                Destroy(_cartStorageCostValueText.gameObject);
            }
        }


        void CreateAndAddCartUI()
        {
            _shoppingCart = FindFirstObjectByType<MarketShoppingCart>(FindObjectsInactive.Include);

            if (_shoppingCart != null)
            {
                GameObject cartWindow = typeof(MarketShoppingCart).GetField("m_CartWindow", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_shoppingCart) as GameObject;

                if (cartWindow != null)
                {
                    RectTransform parent = (RectTransform)cartWindow.transform.GetChild(cartWindow.transform.childCount - 1);

                    // Resize cart scrollview
                    RectTransform cartScrollViewTransform = (RectTransform)parent.transform.GetChild(parent.transform.childCount - 1);
                    
                    if (cartScrollViewTransform != null)
                    {
                        cartScrollViewTransform.sizeDelta = new Vector2(cartScrollViewTransform.sizeDelta.x, 200f);
                        cartScrollViewTransform.offsetMax = new Vector2(cartScrollViewTransform.offsetMax.x, 210f);
                        cartScrollViewTransform.offsetMin = new Vector2(cartScrollViewTransform.offsetMin.x, 35f);
                    }

                    // Add store delivery checkbox
                    GameObject cartToggleContainerObject = Instantiate(Assets.LoadAsset<GameObject>("storeDelivery_cart"), parent);
                    _cartToggleContainerTransform = (RectTransform)cartToggleContainerObject.transform;
                    _cartToggleContainerTransform.localScale = Vector3.one;
                    _cartToggleContainerTransform.anchorMin = Vector2.zero;
                    _cartToggleContainerTransform.anchorMax = Vector2.zero;
                    _cartToggleContainerTransform.pivot = Vector2.zero;
                    _cartToggleContainerTransform.localPosition = Vector3.zero;
                    _cartToggleContainerTransform.localRotation = Quaternion.identity;
                    _cartToggleContainerTransform.anchoredPosition = Vector2.zero;

                    _cartToggle = _cartToggleContainerTransform.GetChild(1).GetChild(0).GetComponent<Toggle>();

                    if (_cartToggle != null) 
                    {
                        _cartToggle.onValueChanged.AddListener(OnStoreDeliveryStatusChanged);
                    }

                    // Set font for label
                    TextMeshProUGUI cartToggleLabelText = _cartToggleContainerTransform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>();
                    if (cartToggleLabelText != null && _font != null)
                    {
                        cartToggleLabelText.font = _font;
                    }

                    // Add cost
                    parent = (RectTransform)parent.transform.GetChild(1);

                    GameObject cartStorageCostLabelObject = Instantiate(Assets.LoadAsset<GameObject>("storeDelivery_storage_text"), parent);
                    _cartStorageCostLabelTransform = (RectTransform)cartStorageCostLabelObject.transform;
                    _cartStorageCostLabelTransform.anchoredPosition = new Vector2(-19.2f, 91.2f);

                    TextMeshProUGUI cartStorageCostLabelText = cartStorageCostLabelObject.GetComponent<TextMeshProUGUI>();

                    if (cartStorageCostLabelText != null && _font != null)
                    {
                        cartStorageCostLabelText.font = _font;
                    }

                    GameObject cartStorageCostValueTextObject = Instantiate(Assets.LoadAsset<GameObject>("storeDelivery_storage_price"), parent);
                    RectTransform cartStorageCostValueTextTransform = (RectTransform)cartStorageCostValueTextObject.transform;
                    cartStorageCostValueTextTransform.anchoredPosition = new Vector2(20.2f, 91.2f);
                    
                    _cartStorageCostValueText = cartStorageCostValueTextObject.GetComponent<TextMeshProUGUI>();

                    if (_cartStorageCostValueText != null && _font != null)
                    {
                        _cartStorageCostValueText.font = _font;
                    }

                    _divideTransform = (RectTransform)parent.Find("Divide");
                    _dividePositionDefault = _divideTransform.anchoredPosition;

                    _totalLabelTransform = (RectTransform)parent.Find("Total");
                    _totalLabelPositionDefault = _totalLabelTransform.anchoredPosition;

                    _totalPriceTransform = (RectTransform)parent.Find("Total Price Text");
                    _totalPricePositionDefault = _totalPriceTransform.anchoredPosition;
                }
            }
        }


        void OnStoreDeliveryStatusChanged(bool value)
        {
            ToggleStorage(value, true);
        }


        public void ToggleStorage(bool value, bool updateShoppingCart)
        {
            _enableStorage = value;

            if (_initialized)
            {
                if (_enableStorage && Plugin.Cfg.ConfigFeesEnabled.Value)
                {
                    ShowStorageCost();
                }
                else
                {
                    HideStorageCost();
                }

                if (updateShoppingCart && _shoppingCart != null)
                {
                    _shoppingCart.UpdateTotalPrice();
                }
            }
        }


        public void UpdateCost(float value)
        {
            StorageCost = (_initialized && _enableStorage) ? value : 0f;

            if (_cartStorageCostValueText != null)
            {
                _cartStorageCostValueText.text = Extensions.ToMoneyText(value, 7f);
            }
        }


        public void ShowStorageCost()
        {
            _cartStorageCostLabelTransform.gameObject.SetActive(true);
            _cartStorageCostValueText.gameObject.SetActive(true);
            _divideTransform.anchoredPosition = new Vector2(_divideTransform.anchoredPosition.x, 79.9f);
            _totalLabelTransform.anchoredPosition = new Vector2(_totalLabelTransform.anchoredPosition.x, 68.6f);
            _totalPriceTransform.anchoredPosition = new Vector2(_totalPriceTransform.anchoredPosition.x, 68.6f);
        }

        public void HideStorageCost()
        {
            _cartStorageCostLabelTransform.gameObject.SetActive(false);
            _cartStorageCostValueText.gameObject.SetActive(false);
            _divideTransform.anchoredPosition = _dividePositionDefault;
            _totalLabelTransform.anchoredPosition = _totalLabelPositionDefault;
            _totalPriceTransform.anchoredPosition = _totalPricePositionDefault;
        }

    }
}
