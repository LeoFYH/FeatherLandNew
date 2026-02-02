using System;
using DG.Tweening;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BirdGame
{
    public class DecorationClickHandler : ViewControllerBase
    {
        public int sceneId;
        private int decorationId;
        public int decorationIndex;
        public bool canFeed;
        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        public DecorationEffect[] effects;

        public ResetSpriteController controller;

        private SpriteRenderer sr;

        public void Initialize(int id, int index)
        {
            decorationId = id;
            decorationIndex = index;
            controller?.SetSp(index);
            
            // 安全访问配置数据，避免索引越界
            var configModel = this.GetModel<IConfigModel>();
            if (configModel?.ShopConfig?.sceneDecorations != null && 
                sceneId >= 0 && sceneId < configModel.ShopConfig.sceneDecorations.Count)
            {
                var sceneDecoration = configModel.ShopConfig.sceneDecorations[sceneId];
                if (sceneDecoration?.decorations != null && 
                    decorationId >= 0 && decorationId < sceneDecoration.decorations.Length)
                {
                    var decoration = sceneDecoration.decorations[decorationId];
                    if (decoration?.fixedPositions != null && 
                        decorationIndex >= 0 && decorationIndex < decoration.fixedPositions.Length)
                    {
                        transform.parent.position = decoration.fixedPositions[decorationIndex];
                    }
                    else
                    {
                        Debug.LogError($"装饰物固定位置索引越界: decorationIndex={decorationIndex}, fixedPositions长度={decoration?.fixedPositions?.Length ?? 0}");
                    }
                }
                else
                {
                    Debug.LogError($"装饰物索引越界: decorationId={decorationId}, decorations长度={sceneDecoration?.decorations?.Length ?? 0}");
                }
            }
            else
            {
                Debug.LogError($"场景装饰索引越界: sceneId={sceneId}, sceneDecorations长度={configModel?.ShopConfig?.sceneDecorations?.Count ?? 0}");
            }
        }

        private void Start()
        {
            this.RegisterEvent<ClearDecorationsEvent>(evt =>
            {
                Destroy(transform.parent.gameObject);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            // this.RegisterEvent<SwitchWeatherEvent>(evt =>
            // {
            //     if (sr == null)
            //         sr = GetComponentInChildren<SpriteRenderer>();
            //     var ani = DOTween.Sequence();
            //     ani.Append(sr.DOColor(Color.black, 0.5f));
            //     ani.Append(sr.DOColor(Color.white, 0.5f));
            // }).UnRegisterWhenGameObjectDestroyed(gameObject);

            if (effects != null && effects.Length > 0)
            {
                for (int i = 0; i < effects.Length; i++)
                {
                    if (effects[i].type == DecorationEffectType.FlyPosition && effects[i].flyPosition != null)
                    {
                        if (!this.GetModel<IBirdModel>().FlyPositions.Contains(effects[i].flyPosition))
                        {
                            this.GetModel<IBirdModel>().FlyPositions.Add(effects[i].flyPosition);
                        }
                    }
                }
            }

           
        }

        // private void OnMouseOver()
        // {
        //     // 检查是否点击到UI元素
        //     if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        //     {
        //         return;
        //     }
        //
        //     // 右键直接销毁装饰品
        //     // 检查标准Unity输入或SimpleMouseForwarder的钩子输入
        //     bool rightClickDetected = Input.GetMouseButtonDown(1) || SimpleMouseForwarder.rightButtonDown;
        //     
        //     if (rightClickDetected)
        //     {
        //         // 重置钩子的右键状态（如果使用了钩子）
        //         if (SimpleMouseForwarder.rightButtonDown)
        //         {
        //             SimpleMouseForwarder.rightButtonDown = false;
        //         }
        //         
        //         DestroyDecoration();
        //     }
        // }

        private void DestroyDecoration()
        {
            // 调用游戏系统的销毁方法
            //this.GetSystem<IGameSystem>().DestroyDecoration(decorationId, gameObject);
            this.GetSystem<IUISystem>().ShowMouseMenu(decorationId, decorationIndex, transform.parent.gameObject);
        }

        private void OnDestroy()
        {
            if (effects != null && effects.Length > 0)
            {
                for (int i = 0; i < effects.Length; i++)
                {
                    if (effects[i].type == DecorationEffectType.FlyPosition && effects[i].flyPosition != null)
                    {
                        if (this.GetModel<IBirdModel>().FlyPositions.Contains(effects[i].flyPosition))
                        {
                            this.GetModel<IBirdModel>().FlyPositions.Remove(effects[i].flyPosition);
                        }
                    }
                }
            }
        }
    }

    [Serializable]
    public class DecorationEffect
    {
        public DecorationEffectType type;

        [ShowIf("@type==DecorationEffectType.FlyPosition")]
        public Transform flyPosition;
    }

    public enum DecorationEffectType
    {
        FlyPosition,
    }
} 