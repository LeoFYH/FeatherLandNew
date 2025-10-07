using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    [RequireComponent(typeof(PolygonCollider2D), typeof(SpriteRenderer))]
    public class DepthMask : ViewControllerBase
    {
        private PolygonCollider2D poly;
        private SpriteRenderer tentRenderer;

        // 用于缓存所有需要参与遮挡的角色
        private List<SpriteRenderer> characterList = new List<SpriteRenderer>();

        void Awake()
        {
            poly = GetComponent<PolygonCollider2D>();
            tentRenderer = GetComponent<SpriteRenderer>();

            // 初始化角色列表（可以指定Tag或自动搜索）
            RefreshCharacters();
        }

        void Update()
        {
            RefreshCharacters();

            foreach (var playerRenderer in characterList)
            {
                if (playerRenderer == null) continue;
                
                if(playerRenderer.sortingOrder != 2 && playerRenderer.sortingOrder != 3 && playerRenderer.sortingOrder != 4)
                    return;

                Vector2 playerFoot = playerRenderer.transform.position;

                // 判断角色脚底是否在帐篷遮挡区域内
                if (poly.OverlapPoint(playerFoot))
                {
                    // 角色在帐篷后面
                    playerRenderer.sortingOrder = tentRenderer.sortingOrder - 1;
                }
                else
                {
                    // 角色在帐篷前面
                    playerRenderer.sortingOrder = tentRenderer.sortingOrder + 1;
                }
            }
        }

        // 手动刷新场景中所有角色
        [ContextMenu("Refresh Characters")]
        public void RefreshCharacters()
        {
            characterList.Clear();

            foreach (var bird in this.GetModel<IBirdModel>().BirdList)
            {
                characterList.Add(bird.bird.sr);
            }
        }
    }
}