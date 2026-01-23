using System;
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
        private List<Brid> characterList = new List<Brid>();
        // Performance optimization: Track if we need to refresh the character list
        private int lastBirdListCount = -1;
        // Performance optimization: Cache bird model reference
        private IBirdModel cachedBirdModel;

        void Awake()
        {
            poly = GetComponent<PolygonCollider2D>();
            tentRenderer = GetComponent<SpriteRenderer>();
            
            // Performance optimization: Cache bird model reference
            cachedBirdModel = this.GetModel<IBirdModel>();

            // 初始化角色列表（可以指定Tag或自动搜索）
            RefreshCharacters();
        }

        void Update()
        {
            // Performance optimization: Only refresh when bird list count changes
            // This avoids expensive refresh every frame
            int currentBirdListCount = cachedBirdModel.BirdList.Count;
            if (currentBirdListCount != lastBirdListCount)
            {
                RefreshCharacters();
                lastBirdListCount = currentBirdListCount;
            }

            foreach (var playerRenderer in characterList)
            {
                if (playerRenderer == null) continue;
                if(playerRenderer.isFlying) continue;
                Vector2 playerFoot = playerRenderer.transform.position;

                // 判断角色脚底是否在帐篷遮挡区域内
                if (poly.OverlapPoint(playerFoot))
                {
                    if (!playerRenderer.maskList.Contains(this) && playerRenderer.sr != null)
                    {
                        playerRenderer.maskList.Add(this);
                        playerRenderer.sr.sortingOrder = 3;
                    }
                }
                else
                {
                    if (playerRenderer.maskList.Contains(this))
                    {
                        playerRenderer.maskList.Remove(this);
                        if (playerRenderer.maskList.Count == 0)
                            playerRenderer.sr.sortingOrder = tentRenderer.sortingOrder + 1;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            RefreshCharacters();
            foreach (var playerRenderer in characterList)
            {
                if (playerRenderer.maskList.Contains(this))
                {
                    playerRenderer.maskList.Remove(this);
                    if (playerRenderer?.maskList.Count == 0)
                        playerRenderer.sr.sortingOrder = tentRenderer.sortingOrder + 1;
                }
            }
        }

        // 手动刷新场景中所有角色
        [ContextMenu("Refresh Characters")]
        public void RefreshCharacters()
        {
            characterList.Clear();

            // Performance optimization: Use cached bird model reference
            foreach (var bird in cachedBirdModel.BirdList)
            {
                characterList.Add(bird.bird);
            }
        }
    }
}