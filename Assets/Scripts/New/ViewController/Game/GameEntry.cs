using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class GameEntry : ViewControllerBase
    {
        private void Start()
        {
            // 延迟一帧来确保所有系统都已初始化
            StartCoroutine(InitializeAfterSystems());
            this.SendCommand<LoadGameCommand>();
        }

        private System.Collections.IEnumerator InitializeAfterSystems()
        {
            // 等待一帧，确保所有系统都已初始化
            yield return null;
            
            // 根据保存的设置设置屏幕模式
            int savedScreenMode = this.GetModel<ISaveModel>().SettingData.screenMode;
            Debug.Log($"从存档加载的屏幕模式: {savedScreenMode}");
            SetScreenMode(savedScreenMode);
            
            //this.GetSystem<ISceneSystem>().LoadScene(0);
        }

        private void SetScreenMode(int mode)
        {
            Debug.Log($"SetScreenMode 被调用，模式: {mode}");
            switch (mode)
            {
                case 0:
                    this.GetUtility<IFullScreenUtility>().WindowedMode();
                    Debug.Log("启动时设置为窗口模式");
                    break;
                case 1:
                    this.GetUtility<IFullScreenUtility>().WallpaperMode();
                    Debug.Log("启动时设置为壁纸模式");
                    break;
                case 2:
                default:
                    this.GetUtility<IFullScreenUtility>().FullscreenMode();
                    Debug.Log("启动时设置为全屏模式");
                    break;
            }
        }

        private void Update()
        {
            
            CheckCursor();
            
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                // 检查是否点击到UI元素
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Click);
                    return;
                }
                
                // 检查是否点击到可点击的物体（如鸟、蛋等）
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D[] hits = Physics2D.RaycastAll(mousePosition, Vector2.zero);
                
                bool clickedOnInteractiveObject = false;
                bool clickedOnBird = false;  // 新增：检测是否点击到鸟
                foreach (var hit in hits)
                {
                    // 检查是否点击到鸟
                    if (hit.collider.CompareTag("Bird"))
                    {
                        clickedOnInteractiveObject = true;
                        clickedOnBird = true;  // 标记点击到鸟
                        break;
                    }
                    
                    // 检查是否点击到蛋
                    if (hit.collider.GetComponent<Egg>() != null)
                    {
                        clickedOnInteractiveObject = true;
                        break;
                    }
                    
                    // 检查是否点击到食物
                    if (hit.collider.GetComponent<Food>() != null)
                    {
                        clickedOnInteractiveObject = true;
                        break;
                    }
                    
                    // 检查是否点击到其他有交互功能的物体
                    if (hit.collider.GetComponent<MonoBehaviour>() != null)
                    {
                        // 这里可以添加更多具体的交互物体检测
                        clickedOnInteractiveObject = true;
                        break;
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
            if (this.GetSystem<IGameSystem>().IsCoverUI())
            {
                this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Click);
            }
            else if (this.GetSystem<IGameSystem>().IsCoverBird())
            {
                this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Click);
            }
            else if (this.GetSystem<IGameSystem>().IsCoverGround())
            {
                this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Feed1);
            }
            else
            {
                this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Default);
            }
        }
    }
}