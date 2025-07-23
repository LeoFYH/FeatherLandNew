using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class CustomCursor : ViewControllerBase
    {
        // [Header("默认鼠标设置")]
        // [Tooltip("支持Texture2D或Sprite")]
        // public Texture2D customCursorTexture;
        // [Tooltip("或者使用Texture2D")]
        // public Texture2D customCursorTexture2;
        // public Vector2 hotSpot = Vector2.zero;
        //
        // [Header("点击状态鼠标")]
        // [Tooltip("点击状态的第一帧")]
        // public Texture2D clickCursorTexture1;
        // [Tooltip("点击状态的第二帧")]
        // public Texture2D clickCursorTexture2;
        // [Tooltip("点击状态鼠标的点击点")]
        // public Vector2 clickHotSpot = Vector2.zero;
        // [Tooltip("点击动画切换间隔（秒）")]
        // public float clickAnimationInterval = 0.1f;
        //
        // [Header("撒食物状态鼠标")]
        // [Tooltip("撒食物状态的第一帧")]
        // public Texture2D foodCursorTexture3;
        // [Tooltip("撒食物状态的第二帧")]
        // public Texture2D foodCursorTexture4;
        // [Tooltip("撒食物状态鼠标的点击点")]
        // public Vector2 foodHotSpot = Vector2.zero;
        //
        // [Header("UI Hover状态鼠标")]
        // [Tooltip("UI Hover状态的鼠标")]
        // public Texture2D uiHoverCursorTexture6;
        // [Tooltip("UI Hover状态鼠标的点击点")]
        // public Vector2 uiHoverHotSpot = Vector2.zero;
        //
        // [Header("自动设置")]
        // public bool setOnStart = true;
        //
        // private bool isInClickMode = false;
        // private bool isInFoodMode = false;
        // private bool isInUIHoverMode = false;
        // private float clickModeEndTimer = 0f;
        // private float foodModeEndTimer = 0f;
        // private float foodReturnTimer = 0f;
        // private bool isWaitingToReturn = false;
        // private bool isWaitingFoodReturn = false;
        // private bool isWaitingToReturnFromFood = false;
        //
        // private void Start()
        // {
        //     if (setOnStart && (customCursorTexture != null || customCursorTexture2 != null))
        //     {
        //         SetCustomCursor();
        //     }
        // }
        //
        // private void Update()
        // {
        //     // 检测是否在UI Hover状态
        //     bool currentlyInUIHoverMode = IsInUIHoverMode();
        //     
        //     // 如果UI Hover状态发生变化
        //     if (currentlyInUIHoverMode != isInUIHoverMode)
        //     {
        //         isInUIHoverMode = currentlyInUIHoverMode;
        //         if (isInUIHoverMode && !isInFoodMode && !isInClickMode && !isWaitingToReturn && !isWaitingToReturnFromFood)
        //         {
        //             // 进入UI Hover状态，切换到帧6
        //             SetUIHoverCursor();
        //         }
        //         else if (!isInUIHoverMode && !isInFoodMode && !isInClickMode && !isWaitingToReturn && !isWaitingToReturnFromFood)
        //         {
        //             // 退出UI Hover状态，恢复默认鼠标
        //             SetCustomCursor();
        //         }
        //     }
        //     
        //     // 检测是否在撒食物状态
        //     bool currentlyInFoodMode = IsInFoodMode();
        //     
        //     // 如果撒食物状态发生变化
        //     if (currentlyInFoodMode != isInFoodMode)
        //     {
        //         isInFoodMode = currentlyInFoodMode;
        //         if (isInFoodMode)
        //         {
        //             // 进入撒食物状态，切换到帧3
        //             SetFoodCursor(true); // true表示使用帧3
        //         }
        //         else
        //         {
        //             // 退出撒食物状态，等待0.5秒后恢复默认鼠标
        //             StartWaitingToReturnFromFood();
        //         }
        //     }
        //     
        //     // 检测是否点击了鼠标左键
        //     if (Input.GetMouseButtonDown(0))
        //     {
        //         if (isInFoodMode && !isWaitingFoodReturn)
        //         {
        //             // 撒食物状态下点击，切换到帧4
        //             StartFoodAnimation();
        //         }
        //         else if (!isInClickMode && !isInFoodMode && !isWaitingToReturnFromFood)
        //         {
        //             // 普通点击，切换到帧2
        //             StartClickAnimation();
        //         }
        //     }
        //     
        //     // 如果在等待返回状态，更新计时器
        //     if (isWaitingToReturn)
        //     {
        //         UpdateWaitingToReturn();
        //     }
        //     
        //     // 如果在等待撒食物返回状态，更新计时器
        //     if (isWaitingFoodReturn)
        //     {
        //         UpdateWaitingFoodReturn();
        //     }
        //     
        //     // 如果在等待从撒食物状态返回，更新计时器
        //     if (isWaitingToReturnFromFood)
        //     {
        //         UpdateWaitingToReturnFromFood();
        //     }
        // }
        //
        // private bool IsInFoodMode()
        // {
        //     // 检测是否在撒食物状态（不在UI上且鼠标左键按下）
        //     return Input.GetMouseButton(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        // }
        //
        // private bool IsInUIHoverMode()
        // {
        //     // 检测是否在UI Hover状态（鼠标在UI上）
        //     return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        // }
        //
        // private void StartClickAnimation()
        // {
        //     isInClickMode = true;
        //     isWaitingToReturn = false;
        //     
        //     // 立即切换到帧2
        //     SetClickCursor(false); // false表示使用帧2
        //     
        //     // 开始等待0.1秒后切换回默认鼠标
        //     StartWaitingToReturn();
        // }
        //
        // private void StartFoodAnimation()
        // {
        //     isWaitingFoodReturn = false;
        //     
        //     // 立即切换到帧4
        //     SetFoodCursor(false); // false表示使用帧4
        //     
        //     // 开始等待0.1秒后切换回帧3
        //     StartWaitingFoodReturn();
        // }
        //
        // private void SetClickCursor(bool useFrame1)
        // {
        //     Texture2D targetTexture = useFrame1 ? clickCursorTexture1 : clickCursorTexture2;
        //     if (targetTexture != null)
        //     {
        //         this.GetSystem<ICursorSystem>().SetCustomCursor(targetTexture, clickHotSpot);
        //     }
        // }
        //
        // private void SetFoodCursor(bool useFrame3)
        // {
        //     Texture2D targetTexture = useFrame3 ? foodCursorTexture3 : foodCursorTexture4;
        //     if (targetTexture != null)
        //     {
        //         this.GetSystem<ICursorSystem>().SetCustomCursor(targetTexture, foodHotSpot);
        //     }
        // }
        //
        // private void SetUIHoverCursor()
        // {
        //     if (uiHoverCursorTexture6 != null)
        //     {
        //         this.GetSystem<ICursorSystem>().SetCustomCursor(uiHoverCursorTexture6, uiHoverHotSpot);
        //     }
        // }
        //
        // private void StartWaitingToReturn()
        // {
        //     isWaitingToReturn = true;
        //     clickModeEndTimer = 0f;
        // }
        //
        // private void UpdateWaitingToReturn()
        // {
        //     clickModeEndTimer += Time.deltaTime;
        //     if (clickModeEndTimer >= 2f) // 
        //     {
        //         // 4秒后恢复默认鼠标
        //         SetCustomCursor();
        //         isWaitingToReturn = false;
        //         isInClickMode = false;
        //     }
        // }
        //
        // private void StartWaitingFoodReturn()
        // {
        //     isWaitingFoodReturn = true;
        //     foodModeEndTimer = 0f;
        // }
        //
        // private void UpdateWaitingFoodReturn()
        // {
        //     foodModeEndTimer += Time.deltaTime;
        //     if (foodModeEndTimer >= 0.1f) // 等待0.1秒
        //     {
        //         // 0.1秒后切换回帧3
        //         SetFoodCursor(true); // true表示使用帧3
        //         isWaitingFoodReturn = false;
        //     }
        // }
        //
        // private void StartWaitingToReturnFromFood()
        // {
        //     isWaitingToReturnFromFood = true;
        //     foodReturnTimer = 0f;
        // }
        //
        // private void UpdateWaitingToReturnFromFood()
        // {
        //     foodReturnTimer += Time.deltaTime;
        //     if (foodReturnTimer >= 0.5f) // 等待0.5秒
        //     {
        //         // 0.5秒后恢复默认鼠标
        //         SetCustomCursor();
        //         isWaitingToReturnFromFood = false;
        //     }
        // }
        //
        // public void SetCustomCursor()
        // {
        //     if (customCursorTexture2 != null)
        //     {
        //         this.GetSystem<ICursorSystem>().SetCustomCursor(customCursorTexture2, hotSpot);
        //     }
        //     else if (customCursorTexture != null)
        //     {
        //         this.GetSystem<ICursorSystem>().SetCustomCursor(customCursorTexture, hotSpot);
        //     }
        // }
        //
        // public void SetDefaultCursor()
        // {
        //     this.GetSystem<ICursorSystem>().SetDefaultCursor();
        // }
        //
        // // 在Inspector中测试的方法
        // [ContextMenu("设置自定义鼠标")]
        // private void SetCustomCursorInInspector()
        // {
        //     SetCustomCursor();
        // }
        //
        // [ContextMenu("恢复默认鼠标")]
        // private void SetDefaultCursorInInspector()
        // {
        //     SetDefaultCursor();
        // }
    }
} 