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
                if(playerRenderer.isFlying) continue;
                Vector2 playerFoot = playerRenderer.transform.position;

                // 判断角色脚底是否在帐篷遮挡区域内
                if (poly.OverlapPoint(playerFoot))
                {
                    if (!playerRenderer.maskList.Contains(this))
                    {
                        playerRenderer.maskList.Add(this);
                        playerRenderer.sr.sortingOrder = tentRenderer.sortingOrder - 1;
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

            foreach (var bird in this.GetModel<IBirdModel>().BirdList)
            {
                characterList.Add(bird.bird);
            }
        }
    }
}