using System;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class GameEntry : ViewControllerBase
    {
        public Action onUpdate;
        
        // Cooldown timer to prevent rapid mode switching during window state transitions
        private float _lastModeChangeTime = -999f;
        private const float MODE_CHANGE_COOLDOWN = 0.5f; // 500ms cooldown between mode changes
        
        private void Start()
        {
            // 延迟一帧来确保所有系统都已初始化
            StartCoroutine(InitializeAfterSystems());
            this.SendCommand<LoadGameCommand>();
        }

        private System.Collections.IEnumerator InitializeAfterSystems()
        {
            // 等待一帧，确保所有系统都已初始化
            while (this.GetModel<ISaveModel>().SettingData == null)
            {
                yield return null;
            }
            // 根据保存的设置设置屏幕模式
            this.GetModel<ISaveModel>().SettingData.screenMode = 2;
            int savedScreenMode = this.GetModel<ISaveModel>().SettingData.screenMode;
            Debug.Log($"从存档加载的屏幕模式: {savedScreenMode}");
            SetScreenMode(savedScreenMode, forceChange: true); // Force initial mode setting
            
            //this.GetSystem<ISceneSystem>().LoadScene(0);
        }

        /// <summary>
        /// 切换屏幕模式（统一方法，供启动和快捷键调用）
        /// </summary>
        /// <param name="mode">模式：0=窗口模式, 1=壁纸模式, 2=全屏模式</param>
        /// <param name="forceChange">是否强制切换（忽略冷却时间，用于初始化）</param>
        private void SetScreenMode(int mode, bool forceChange = false)
        {
            // Check cooldown to prevent rapid mode switching during transitions
            if (!forceChange && Time.time - _lastModeChangeTime < MODE_CHANGE_COOLDOWN)
            {
                Debug.Log($"SetScreenMode 被忽略（冷却中）- 模式: {mode}, 剩余冷却时间: {MODE_CHANGE_COOLDOWN - (Time.time - _lastModeChangeTime):F2}秒");
                return;
            }
            
            // Check if already in the requested mode
            int currentMode = this.GetModel<ISaveModel>().SettingData?.screenMode ?? -1;
            if (!forceChange && currentMode == mode)
            {
                Debug.Log($"SetScreenMode 被忽略（已处于模式 {mode}）");
                return;
            }
            
            Debug.Log($"SetScreenMode 被调用，模式: {mode}");
            
            // Clear keyboard state before mode change to prevent lingering key states
            SimpleMouseForwarder.ClearKeyboardState();
            
            switch (mode)
            {
                case 0:
                    this.GetUtility<IFullScreenUtility>().WindowedMode();
                    Debug.Log("设置为窗口模式");
                    break;
                case 1:
                    this.GetUtility<IFullScreenUtility>().WallpaperMode();
                    Debug.Log("设置为壁纸模式");
                    break;
                default:
                    this.GetUtility<IFullScreenUtility>().FullscreenMode();
                    Debug.Log("设置为全屏模式");
                    break;
            }
            
            // Clear keyboard state after mode change as well
            SimpleMouseForwarder.ClearKeyboardState();
            
            // Update cooldown timer
            _lastModeChangeTime = Time.time;
            
            // 保存设置到存档
            if (this.GetModel<ISaveModel>().SettingData != null)
            {
                this.GetModel<ISaveModel>().SettingData.screenMode = mode;
            }
            
            // 发送事件通知UI更新
            this.GetSystem<IMonoSystem>().SendEvent(new ChangeScreenModeEvent { mode = mode });
        }

        private void Update()
        {
            onUpdate?.Invoke();
            this.GetSystem<ISteamSystem>().RunCallbacks();
            CheckCursor();
            
            // 检测快捷键（仅在非输入状态下）
            HandleKeyboardShortcuts();
            
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                // 检查是否点击到UI元素
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null &&
                        UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.CompareTag("Click"))
                        this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Click);
                    return;
                }
                
                // 检查是否点击到可点击的物体（如鸟、蛋等）
                Vector2 mousePosition = Input.mousePosition;
                
                // 获取主摄像机
                Camera mainCamera = Camera.main;
                bool clickedOnInteractiveObject = false;
                bool clickedOnBird = false;  // 新增：检测是否点击到鸟
                
                if (mainCamera != null)
                {
                    // 将鼠标位置转换为世界坐标
                    Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, -mainCamera.transform.position.z));
                    
                    // 检查鸟
                    GameObject[] birds = GameObject.FindGameObjectsWithTag("Bird");
                    foreach (var bird in birds)
                    {
                        if (bird == null) continue;
                        
                        Collider2D collider2D = bird.GetComponent<Collider2D>();
                        
                        if (collider2D != null)
                        {
                            if (collider2D.OverlapPoint(worldPosition))
                            {
                                clickedOnInteractiveObject = true;
                                clickedOnBird = true;
                                break;
                            }
                        }
                        else
                        {
                            float distance = Vector2.Distance(worldPosition, bird.transform.position);
                            if (distance < 0.5f)
                            {
                                clickedOnInteractiveObject = true;
                                clickedOnBird = true;
                                break;
                            }
                        }
                    }
                    
                    // 检查蛋
                    if (!clickedOnBird)
                    {
                        GameObject[] eggs = GameObject.FindGameObjectsWithTag("Egg");
                        foreach (var egg in eggs)
                        {
                            if (egg == null) continue;
                            
                            Collider2D collider2D = egg.GetComponent<Collider2D>();
                            
                            if (collider2D != null)
                            {
                                if (collider2D.OverlapPoint(worldPosition))
                                {
                                    clickedOnInteractiveObject = true;
                                    break;
                                }
                            }
                            else
                            {
                                float distance = Vector2.Distance(worldPosition, egg.transform.position);
                                if (distance < 0.5f)
                                {
                                    clickedOnInteractiveObject = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    // 检查食物和其他物体
                    if (!clickedOnBird)
                    {
                        try
                        {
                            GameObject[] foods = GameObject.FindGameObjectsWithTag("Food");
                            foreach (var food in foods)
                            {
                                if (food == null) continue;

                                Collider2D collider2D = food.GetComponent<Collider2D>();

                                if (collider2D != null)
                                {
                                    if (collider2D.OverlapPoint(worldPosition))
                                    {
                                        clickedOnInteractiveObject = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    float distance = Vector2.Distance(worldPosition, food.transform.position);
                                    if (distance < 0.5f)
                                    {
                                        clickedOnInteractiveObject = true;
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
                
                // 只有在点击到UI或可交互物体时才播放音效，但鸟除外
                if (clickedOnInteractiveObject && !clickedOnBird)
                {
                    this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Click);
                }
            }
        }

        private void CheckCursor()
        {
            if(this.GetSystem<ICursorSystem>().IsPlayingAnim())
                return;
                
            bool isCoverUI = this.GetSystem<IGameSystem>().IsCoverUI();
            bool isCoverBird = this.GetSystem<IGameSystem>().IsCoverBird();
            bool isCoverGround = this.GetSystem<IGameSystem>().IsCoverGround();
            
            // 调试信息
            if (isCoverBird)
            {
                //Debug.Log("检测到鸟，设置cursor为Click状态");
            }
            
            if (isCoverUI)
            {
                this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Click);
            }
            else if (isCoverBird)
            {
                this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Click);
            }
            else if (isCoverGround)
            {
                this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Feed1);
            }
            else
            {
                this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Default);
            }
        }
        
        /// <summary>
        /// 处理键盘快捷键
        /// </summary>
        private void HandleKeyboardShortcuts()
        {
            // 检查是否有输入框处于焦点状态，如果有则不处理快捷键
            if (IsInputFieldFocused())
            {
                return;
            }

            // 检测屏幕模式切换快捷键：1=窗口模式, 2=壁纸模式, 3=全屏模式
            // Cache key states to avoid multiple calls and race conditions
            bool key1Down = GetKeyDownAny(KeyCode.Alpha1) || GetKeyDownAny(KeyCode.Keypad1);
            bool key2Down = GetKeyDownAny(KeyCode.Alpha2) || GetKeyDownAny(KeyCode.Keypad2);
            bool key3Down = GetKeyDownAny(KeyCode.Alpha3) || GetKeyDownAny(KeyCode.Keypad3);
            
            if (key1Down)
            {
                SetScreenMode(0);
                return; // Early return to prevent processing other keys in the same frame
            }
            
            if (key2Down)
            {
                SetScreenMode(1);
                return; // Early return to prevent processing other keys in the same frame
            }
            
            if (key3Down)
            {
                SetScreenMode(2);
                return; // Early return to prevent processing other keys in the same frame
            }
            
            // 获取MenuPanel以便访问toggle按钮
            var menuPanel = this.GetSystem<IUISystem>().GetPanel<MenuPanel>();
            if (menuPanel == null)
            {
                return; // MenuPanel未加载，不处理快捷键
            }

            // 检测快捷键按下（使用 GetKeyDown 确保每次按键只触发一次）
            // 直接切换toggle按钮状态，而不是调用TogglePopup
            if (GetKeyDownAny(KeyCode.N))
            {
                // Notebook - NotePopup
                menuPanel.noteToggle.isOn = !menuPanel.noteToggle.isOn;
            }
            else if (GetKeyDownAny(KeyCode.R))
            {
                // Radio - RadioPopup
                menuPanel.radioToggle.isOn = !menuPanel.radioToggle.isOn;
            }
            else if (GetKeyDownAny(KeyCode.P))
            {
                // Pomodoro Clock - ClockPopup
                menuPanel.clockToggle.isOn = !menuPanel.clockToggle.isOn;
            }
            else if (GetKeyDownAny(KeyCode.M))
            {
                // Map - MapPopup
                menuPanel.mapButton.isOn = !menuPanel.mapButton.isOn;
            }
            else if (GetKeyDownAny(KeyCode.S))
            {
                // Shop - ShopPopup
                menuPanel.shopButton.isOn = !menuPanel.shopButton.isOn;
            }
            else if (GetKeyDownAny(KeyCode.B))
            {
                // Birdbook - IllustratedPopup
                menuPanel.illustratedButton.isOn = !menuPanel.illustratedButton.isOn;
            }
            else if (GetKeyDownAny(KeyCode.T))
            {
                // Tutorial - TutorialPopup (没有对应的toggle按钮，保持原有行为)
                this.GetSystem<IUISystem>().TogglePopup(UIPopup.TutorialPopup);
            }
        }
        
        /// <summary>
        /// Helper method to check key press from both Unity Input and Windows Hook
        /// Uses OR logic to support both input systems but prevents double-detection
        /// </summary>
        private bool GetKeyDownAny(KeyCode keyCode)
        {
            // Check both input systems
            // In windowed/fullscreen: Unity Input will work
            // In wallpaper mode: Windows Hook will work (and Unity Input may or may not)
            // Using OR ensures at least one system detects the key
            bool unityInput = Input.GetKeyDown(keyCode);
            bool hookInput = SimpleMouseForwarder.GetKeyDown(keyCode);
            
            return unityInput || hookInput;
        }

        /// <summary>
        /// 检查是否有输入框处于焦点状态
        /// </summary>
        private bool IsInputFieldFocused()
        {
            // 检查 EventSystem 当前选中的对象
            GameObject selectedObject = EventSystem.current?.currentSelectedGameObject;
            if (selectedObject != null)
            {
                // 检查是否是 TMP_InputField
                if (selectedObject.GetComponent<TMP_InputField>() != null)
                {
                    TMP_InputField tmpInput = selectedObject.GetComponent<TMP_InputField>();
                    if (tmpInput != null && tmpInput.isFocused)
                    {
                        return true;
                    }
                }

                // 检查是否是 InputField
                if (selectedObject.GetComponent<InputField>() != null)
                {
                    InputField input = selectedObject.GetComponent<InputField>();
                    if (input != null && input.isFocused)
                    {
                        return true;
                    }
                }
            }

            // 检查所有 TMP_InputField
            TMP_InputField[] tmpInputFields = FindObjectsOfType<TMP_InputField>();
            foreach (var inputField in tmpInputFields)
            {
                if (inputField != null && inputField.isFocused)
                {
                    return true;
                }
            }

            // 检查所有 InputField
            InputField[] inputFields = FindObjectsOfType<InputField>();
            foreach (var inputField in inputFields)
            {
                if (inputField != null && inputField.isFocused)
                {
                    return true;
                }
            }

            return false;
        }
        
        void OnApplicationQuit()
        {
            this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
            this.GetSystem<ISteamSystem>().ShutDown();
        }
    }
}