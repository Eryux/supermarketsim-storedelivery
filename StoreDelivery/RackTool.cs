// Copyright(c) 2024 - C.Nicolas <contact@bark.tf>
// github.com/eryux

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using MyBox;

namespace StoreDelivery
{
    public class RackTool
    {
        public static RackSlot GetRackSlotFor(Box box, bool allowEmpty = false, bool allowReplace = false)
        {
            if (box.HasProducts)
            {
                RackManager rackManager = MyBox.Singleton<RackManager>.Instance;

                if (rackManager != null)
                {
                    List<Rack> racks = typeof(RackManager).GetField("m_Racks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rackManager) as List<Rack>;

                    RackSlot slot = racks.SelectMany((Rack rack) => rack.RackSlots).FirstOrDefault((RackSlot s) => s.Data.ProductID == box.Product.ID && !s.Full);

                    if (slot == null && allowEmpty)
                    {
                        slot = racks.SelectMany((Rack rack) => rack.RackSlots).FirstOrDefault((RackSlot s) => s.Data.ProductID < 1 && s.CurrentBoxID == -1 && !s.HasBox);
                    }

                    if (slot == null && allowReplace)
                    {
                        slot = racks.SelectMany((Rack rack) => rack.RackSlots).FirstOrDefault((RackSlot s) => !s.HasBox);
                    }

                    return slot;
                }
            }

            return null;
        }


        public static void PlaceBoxInRack(RackSlot rackSlot, Box box)
        {
            Collider[] componentsInChildren = box.GetComponentsInChildren<Collider>();
            for (int i = 0; i < componentsInChildren.Length; i++) {
                componentsInChildren[i].isTrigger = false;
            }

            box.SetOccupy(false, null);
            box.FrezeeBox();

            // This part is a partial re-write of RackSlot.AddBox method
            // --
            // We don't use RackSlot.AddBox directly to avoid box animation during placement which cause
            // problem with boxes sorting.

            RackSlotData m_Data = rackSlot.Data;
            if (m_Data.RackedBoxDatas == null || m_Data.BoxCount <= 0)
            {
                if (rackSlot.HasLabel) {
                    Singleton<RackManager>.Instance.RemoveRackSlot(m_Data.ProductID, rackSlot);
                }

                m_Data.Clear();
                m_Data.Setup(box.Data.ProductID, box.BoxID);
                Singleton<RackManager>.Instance.AddRackSlot(m_Data.ProductID, rackSlot);
                typeof(RackSlot).GetMethod("SetLabel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rackSlot, null);
            }

            box.transform.SetParent(rackSlot.transform);
            box.transform.localPosition = ItemPosition.GetPosition(Singleton<IDManager>.Instance.BoxSO(box.BoxID).GridLayout, m_Data.BoxCount);
            box.transform.localRotation = Quaternion.Euler(Singleton<IDManager>.Instance.BoxSO(box.BoxID).GridLayout.boxAngle);

            float localScale = Singleton<IDManager>.Instance.BoxSO(box.BoxID).GridLayout.scaleMultiplier;
            box.transform.localScale = new Vector3(localScale, localScale, localScale);

            Collider collider;
            if (box.TryGetComponent<Collider>(out collider)) {
                collider.enabled = true;
            };

            ((List<Box>)typeof(RackSlot).GetField("m_Boxes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rackSlot)).Add(box);
            ((List<BoxData>)typeof(RackSlotData).GetField("RackedBoxDatas", BindingFlags.Instance | BindingFlags.Public).GetValue(m_Data)).Add(box.Data); // m_Data.RackedBoxDatas.Add(box.Data) should be better but VS Studio typing it incorrectly
            ((Highlightable)typeof(RackSlot).GetField("m_Highlightable", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rackSlot)).AddOrRemoveRenderer(box.RenderersToHighlight, true);
            ((Rack)typeof(RackSlot).GetField("m_Rack", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rackSlot)).AddOrRemoveRenderer(box.RenderersToHighlight, true);
            Singleton<InventoryManager>.Instance.RemoveBox(box.Data);
            ((Label)typeof(RackSlot).GetField("m_Label", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rackSlot)).ProductCount = m_Data.TotalProductCount;

            // --

            // Set box racked state
            box.Racked = true;
            box.gameObject.layer = LayerMask.NameToLayer("Interactable");
        }


        public static void SortBoxesOnRackSlot(RackSlot rackSlot)
        {
            FieldInfo m_BoxesField = typeof(RackSlot).GetField("m_Boxes", BindingFlags.Instance | BindingFlags.NonPublic);

            List<Box> sortedBoxes = ((List<Box>) m_BoxesField.GetValue(rackSlot)).OrderByDescending(x => x.Data.ProductCount).ToList();
            m_BoxesField.SetValue(rackSlot, sortedBoxes);

            List<BoxData> sortedBoxData = new List<BoxData>();
            for (int i = 0; i < sortedBoxes.Count; ++i) 
            {
                sortedBoxData.Add(sortedBoxes[i].Data);
                sortedBoxes[i].transform.localPosition = ItemPosition.GetPosition(Singleton<IDManager>.Instance.BoxSO(sortedBoxes[i].BoxID).GridLayout, i);
            }
            rackSlot.Data.RackedBoxDatas = sortedBoxData;
        }
    }
}
