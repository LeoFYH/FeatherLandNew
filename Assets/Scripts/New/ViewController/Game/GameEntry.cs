using System;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class GameEntry : ViewControllerBase
    {
        private void Start()
        {
            this.GetSystem<ISceneSystem>().LoadScene(0);
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
                foreach (var hit in hits)
                {
                    // 检查是否点击到鸟
                    if (hit.collider.CompareTag("Bird"))
                    {
                        clickedOnInteractiveObject = true;
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
                
                // 只有在点击到UI或可交互物体时才播放音效
                if (clickedOnInteractiveObject)
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