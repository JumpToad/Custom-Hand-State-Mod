using BasicUI;
using HarmonyLib;
using MoveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;
using Color = UnityEngine.Color;

namespace CustomHandstate
{
    public class CustomHandStateMod : IMod
    {
        public void Initialize()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "com.duckly.CustomHandstate");
            GameObject go = new GameObject("HandStateManager");
            go.AddComponent<HandStateManager>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        public static Color HexToColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }

    public class HandStateManager : MonoBehaviour
    {
        public static Dictionary<int, ICustomHandState> AllHandStates { get; private set; } = new Dictionary<int, ICustomHandState>();

        public static void Initialize()
        {
            foreach (LoadedModDll dlls in ModManager.loadedModDlls)
            {
                Type[] types = dlls.modAssembly.GetTypes();
                foreach (Type type in types)
                {
                    if (typeof(ICustomHandState).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
                    {
                        ICustomHandState stateInstance = (ICustomHandState)Activator.CreateInstance(type);
                        AllHandStates.Add(stateInstance.Id, stateInstance);
                    }
                }
            }
            ReplaceHands();
            Debug.Log("[CustomHandState] Mod initialized.");
        }

        public static void ReplaceHands()
        {
            foreach (Hand hand in Resources.FindObjectsOfTypeAll(typeof(Hand)))
            {
                if (hand.GetType() == typeof(CustomHand)) { continue; }
                CustomHand handNew = hand.gameObject.AddComponent<CustomHand>();
                handNew.bodypartRigidbody = hand.bodypartRigidbody;
                handNew.equipmentPosition = hand.equipmentPosition;
                handNew.spawnPosition = hand.spawnPosition;
                handNew.handColliders = hand.handColliders;
                handNew.handTrigger = hand.handTrigger;
                handNew.placeholderGameObject = hand.placeholderGameObject;
                DestroyImmediate(hand);
            }
        }
    }

    public interface ICustomHandState
    {
        int Id { get; }
        string Name { get; }
        Color RGB { get; }

        void Initialize(CustomHand hand);
        void OnStateEnter(CustomHand hand);
        void OnStateExit(CustomHand hand);
        void FixedUpdate(CustomHand hand);
    }

    public class CustomHand : Hand
    {
        public Dictionary<string, object> CustomHandStateData = new Dictionary<string, object>();

        public override void Start()
        {
            base.Start();
            foreach (ICustomHandState state in HandStateManager.AllHandStates.Values)
            {
                state.Initialize(this);
            }
        }

        private void FixedUpdate()
        {
            foreach (ICustomHandState state in HandStateManager.AllHandStates.Values)
            {
                state.FixedUpdate(this);
            }
        }
    }

    public class HandStatePageManager : MonoBehaviour
    {
        private int currentPageIndex = 0;
        private List<List<GameObject>> pages = new List<List<GameObject>>(4);

        private void Start()
        {
            pages.Add(transform.Cast<Transform>().Select(t => t.gameObject).ToList());
            for (int i = 0; i < (HandStateManager.AllHandStates.Count + 3) / 4; i++)
            {
                pages.Add(new List<GameObject>());
            }
            GameObject template = Instantiate(transform.Find("HoldButton").gameObject);
            template.SetActive(false);
            Destroy(template.GetComponentInChildren<LocalizeStringEvent>());
            template.GetComponent<Button>().onClick.RemoveAllListeners();
            foreach (ICustomHandState state in HandStateManager.AllHandStates.Values.ToArray())
            {
                foreach (List<GameObject> page in pages)
                {
                    if (page.Count < 4)
                    {
                        GameObject button = Instantiate(template, transform);
                        page.Add(button);
                        button.transform.localPosition = new Vector2(button.transform.localPosition.x, button.transform.localPosition.y - (page.Count - 1) * 30f);
                        button.GetComponent<Button>().onClick.AddListener(delegate
                        {
                            Traverse.Create(MoveSetEditor.singleton).Method("SetHandHoldState", (HandState)state.Id).GetValue();
                        });
                        button.GetComponentInChildren<Text>().text = state.Name;
                        button.name = state.Name + "Button";
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (transform.parent.gameObject.activeSelf)
            {
                if (Input.mouseScrollDelta.y > 0f && currentPageIndex > 0) { currentPageIndex--; }
                else if (Input.mouseScrollDelta.y < 0f && currentPageIndex < pages.Count - 1) { currentPageIndex++; }
            }

            foreach (List<GameObject> page in pages)
            {
                foreach (GameObject button in page)
                {
                    if (pages.IndexOf(page) == currentPageIndex) { button.SetActive(true); }
                    else { button.SetActive(false); }
                }
            }
        }

        private void OnDisable()
        {
            currentPageIndex = 0;
        }
    }

    [HarmonyPatch(typeof(ModManager), "ModLoadEndChecks")]
    public static class ModLoadedPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            HandStateManager.Initialize();
        }
    }

    [HarmonyPatch(typeof(Hand), "SetHandState")]
    public static class SetHandStatePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Hand __instance, HandState newHandState)
        {
            if (__instance.GetType() != typeof(CustomHand)) { return true; }

            CustomHand hand = (CustomHand)__instance;
            int idNew = (int)newHandState;
            if ((int)hand.handState != idNew)
            {
                if ((int)hand.handState > 2) { HandStateManager.AllHandStates[(int)hand.handState].OnStateExit(hand); }
                if (idNew > 2) { HandStateManager.AllHandStates[idNew].OnStateEnter(hand); }
                if (idNew < 3) { return true; }
                hand.handState = (HandState)idNew;
            }
            return false;
        }
    }

    [HarmonyPatch]
    public static class MoveSetEditorPatch
    {
        [HarmonyPatch(typeof(MoveSetEditor), "UpdateTimeLineMoves")]
        [HarmonyPrefix]
        public static bool Prefix(MoveSetEditor __instance)
        {
            float num = 200f;
            Traverse.Create(__instance).Method("ClearTimeLine").GetValue();

            RectTransform timeLineRowRectTransform = Traverse.Create(__instance).Field<RectTransform>("timeLineRowRectTransform").Value;
            timeLineRowRectTransform.sizeDelta = new Vector2(Screen.width - 20, num);

            RectTransform timeLineJointRectTransform = Traverse.Create(__instance).Field<RectTransform>("timeLineJointRectTransform").Value;
            timeLineJointRectTransform.sizeDelta = new Vector2(Screen.width - 20 - 14, num);

            Move selectedMove = Traverse.Create(__instance).Field<Move>("selectedMove").Value;
            RectTransform timeLineRectTransform = Traverse.Create(__instance).Field<RectTransform>("timeLineRectTransform").Value;
            if (selectedMove != null)
            {
                if (selectedMove.jointMoveList != null)
                {
                    List<JointType> selectedJointTypes = Traverse.Create(__instance).Field<List<JointType>>("selectedJointTypes").Value;
                    bool flag = selectedJointTypes.Count > 0 && selectedJointTypes.Count < Traverse.Create(__instance).Field<List<MultiselectItem>>("jointFilterItems").Value.Count;
                    int num2 = 0;
                    foreach (JointMove move in selectedMove.jointMoveList)
                    {
                        if (flag && !selectedJointTypes.Contains(move.joint))
                        {
                            continue;
                        }
                        MoveDot moveDotFromPool = (MoveDot)Traverse.Create(__instance).Method("GetMoveDotFromPool").GetValue();
                        moveDotFromPool.Enable();
                        _ = moveDotFromPool.rectTransform;
                        float num3 = Convert.ToSingle(timeLineJointRectTransform.sizeDelta.x * (move.executionTime / (double)selectedMove.duration));
                        float num4 = -6f;
                        moveDotFromPool.SetPosition(num3, num4);
                        for (int i = 0; i < __instance.moveDots.Count; i++)
                        {
                            MoveDot moveDot = __instance.moveDots[i];
                            if (moveDot.positionY <= num4 && num3 - 10f < moveDot.positionX && moveDot.positionX < num3 + 10f)
                            {
                                num4 = moveDot.positionY - 12f;
                                moveDotFromPool.SetPosition(num3, num4);
                            }
                        }
                        moveDotFromPool.UpdatePosition();
                        moveDotFromPool.dotImage.color = Color.white;
                        if (move.handState.HasValue)
                        {
                            if (move.handState == HandState.Hold)
                            {
                                moveDotFromPool.dotImage.color = UISettings.HandHoldColor;
                            }
                            else if (move.handState == HandState.LooseHold)
                            {
                                moveDotFromPool.dotImage.color = UISettings.HandLooseHoldColor;
                            }
                            else if (move.handState == HandState.NoHold)
                            {
                                moveDotFromPool.dotImage.color = UISettings.HandNoHoldColor;
                            }
                            else 
                            {
                                moveDotFromPool.dotImage.color = HandStateManager.AllHandStates[(int)move.handState].RGB;
                            }
                        }
                        if (move == __instance.selectedSingleMove)
                        {
                            moveDotFromPool.dotImage.color = Color.yellow;
                        }
                        else if (Traverse.Create(__instance).Field<List<JointMove>>("selectedJointMoves").Value.Where((JointMove jointMove) => jointMove == move).FirstOrDefault() != null)
                        {
                            moveDotFromPool.dotImage.color = Color.red;
                        }
                        moveDotFromPool.SingleMove = move;
                        moveDotFromPool.tooltipItem.text = move.joint.GetDescription();
                        __instance.moveDots.Add(moveDotFromPool);
                        if (Math.Abs(num4) + 6f > num)
                        {
                            num = Math.Abs(num4) + 6f;
                        }
                        num2++;
                    }
                }
                float x2 = Convert.ToSingle(timeLineJointRectTransform.sizeDelta.x * (__instance.timeLineSlider.value / selectedMove.duration)) + 7f;
                float y = 0f;
                timeLineRectTransform.anchoredPosition = new Vector3(x2, y, 0f);
                timeLineRectTransform.sizeDelta = new Vector2(1f, num);
            }
            else
            {
                timeLineRectTransform.anchoredPosition = new Vector3(-1000f, 0f, 0f);
            }
            Traverse.Create(__instance).Field<RectTransform>("timeLineHolderRectTransform").Value.sizeDelta = new Vector2(0f, num - 200f);
            timeLineRowRectTransform.sizeDelta = new Vector2(Screen.width - 20, num);
            timeLineJointRectTransform.sizeDelta = new Vector2(Screen.width - 20 - 14, num);
            Traverse.Create(__instance).Method("UpdateHandStateUI").GetValue();
            Traverse.Create(__instance).Method("UpdateAnimation").GetValue();
            return false;
        }

        [HarmonyPatch(typeof(MoveSetEditor), "Awake")]
        [HarmonyPostfix]
        public static void Postfix()
        {
            UnityEngine.Object.FindAnyObjectByType<HandStateSelectPanel>(FindObjectsInactive.Include).transform.Find("HandHoldPanel").gameObject.AddComponent<HandStatePageManager>();
            HandStateManager.ReplaceHands();
        }
    }

    [HarmonyPatch(typeof(EnumExtensions), "GetDescription")]
    public static class GetDescriptionPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Enum enumValue, ref string __result)
        {
            int index = Convert.ToInt32(enumValue);
            if (enumValue.GetType() == typeof(HandState) && index > 2)
            {
                __result = HandStateManager.AllHandStates[index].Name;
                return false;
            }
            return true;
        }
    }
}