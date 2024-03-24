// Copyright(c) 2024 - C.Nicolas <contact@bark.tf>
// github.com/eryux

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

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
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].isTrigger = false;
            }

            Rigidbody boxPhysics;
            if (box.TryGetComponent<Rigidbody>(out boxPhysics))
            {
                boxPhysics.isKinematic = true;
                boxPhysics.velocity = Vector3.zero;
                boxPhysics.interpolation = RigidbodyInterpolation.None;
            }

            box.gameObject.layer = LayerMask.NameToLayer("Interactable");

            rackSlot.AddBox(box.BoxID, box);

            box.Racked = true;
        }
    }
}
