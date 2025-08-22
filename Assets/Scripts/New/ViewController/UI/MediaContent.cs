using System;
using DG.Tweening;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class MediaContent : ViewControllerBase
    {
        public Button redNoteButton;
        public Button insButton;
        public Button discordButton;
        public Button xButton;
        public Button emailButton;
        public Button questionButton;
        public Button steamButton;

        private void Start()
        {
            redNoteButton.onClick.AddListener(() =>
            {
                this.GetSystem<IGameSystem>().OpenUrl("https://www.xiaohongshu.com/");
            });
            insButton.onClick.AddListener(() =>
            {
                this.GetSystem<IGameSystem>().OpenUrl("https://www.instagram.com/");
            });
            discordButton.onClick.AddListener(() =>
            {
                this.GetSystem<IGameSystem>().OpenUrl("https://discord.com/");
            });
            xButton.onClick.AddListener(() =>
            {
                this.GetSystem<IGameSystem>().OpenUrl("https://www.x.com/");
            });
            emailButton.onClick.AddListener(() =>
            {
                this.GetSystem<IGameSystem>().OpenUrl("https://mail.qq.com/");
            });
            questionButton.onClick.AddListener(() =>
            {
                
            });
            steamButton.onClick.AddListener(() =>
            {
                this.GetSystem<IGameSystem>().OpenUrl("https://store.steampowered.com/");
            });

            var rect = GetComponent<RectTransform>();
            rect.DOAnchorPosX(-rect.sizeDelta.x * 0.5f, 0.3f);
        }
    }
}