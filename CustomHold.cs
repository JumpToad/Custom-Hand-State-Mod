using CustomHandstate;
using HarmonyLib;
using UnityEngine;

namespace CustomHandState
{
    public class TemplateHold : ICustomHandState
    {
        public int Id { get; } = 3;
        public string Name { get; } = "Template Hold";
        public Color RGB { get; } = Color.blue;

        public void OnStateEnter(CustomHand hand)
        {
            Debug.Log("[CustomHandState] Template holdstate entered!");

            if (hand.joint != null && hand.currentlyGrabbedItem != null)
            {
                hand.joint.zMotion = ConfigurableJointMotion.Locked;
                Vector3? holdPosition = hand.currentlyGrabbedItem.GetHoldPosition(hand.spawnPosition.position, hand, force: true);
                if (holdPosition.HasValue)
                {
                    hand.joint.anchor = holdPosition.Value;
                }
                else
                {
                    Traverse.Create(hand).Method("SetJointToNoHold").GetValue();
                }
            }

            if (hand.CustomHandStateData.ContainsKey("MyCustomField"))
            {
                Debug.Log("[CustomHandState] Detected hand state custom field: " + hand.CustomHandStateData["MyCustomField"].ToString());
                hand.CustomHandStateData.Remove("MyCustomField");
            }
        }

        public void OnStateExit(CustomHand hand)
        {
            Debug.Log("[CustomHandState] Template holdstate exited!");
            hand.CustomHandStateData["MyCustomField"] = Random.Range(0f, 100f);
        }

        public void FixedUpdate(CustomHand hand)
        {
            Debug.Log("[CustomHandState] Fixed update on TemplateHold called!");

            if ((int)hand.handState == Id)
            {
                Debug.Log("[CustomHandState] The current hand state is TemplateHold!");
            }
        }
    }
}
