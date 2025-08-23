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

        // 添加防重复点击的变量
        private float lastClickTime = 0f;
        private const float CLICK_COOLDOWN = 1f; // 1秒冷却时间

        private void Start()
        {
            redNoteButton.onClick.AddListener(() =>
            {
                OpenUrlWithCooldown("https://www.xiaohongshu.com/user/profile/62f42f6b000000001f007537?xsec_token=ABG-fPIZWmANGh5VbS-JNNtIrrKltyZkjw1r8LwLOI3Q8=&xsec_source=pc_note");
            });
            insButton.onClick.AddListener(() =>
            {
                OpenUrlWithCooldown("https://www.instagram.com/featherlandofficial/");
            });
            discordButton.onClick.AddListener(() =>
            {
                OpenUrlWithCooldown("https://discord.gg/dHJ4zfAzpn");
            });
            xButton.onClick.AddListener(() =>
            {
                OpenUrlWithCooldown("https://www.x.com/");
            });
            emailButton.onClick.AddListener(() =>
            {
                OpenUrlWithCooldown("https://mail.qq.com/");
            });
            questionButton.onClick.AddListener(() =>
            {
                
            });
            steamButton.onClick.AddListener(() =>
            {
                OpenUrlWithCooldown("https://store.steampowered.com/");
            });

            var rect = GetComponent<RectTransform>();
            rect.DOAnchorPosX(-rect.sizeDelta.x * 0.5f, 0.3f);
        }

        /// <summary>
        /// 带冷却时间的URL打开方法，确保每次点击都能正常跳转
        /// </summary>
        /// <param name="url">要打开的URL</param>
        private void OpenUrlWithCooldown(string url)
        {
            // 检查是否在冷却时间内
            if (Time.time - lastClickTime < CLICK_COOLDOWN)
            {
                Debug.Log("点击过于频繁，请稍后再试");
                return;
            }

            // 更新最后点击时间
            lastClickTime = Time.time;

            // 添加随机参数确保URL唯一性
            string uniqueUrl = url;
            if (url.Contains("?"))
            {
                uniqueUrl += "&_t=" + System.DateTime.Now.Ticks;
            }
            else
            {
                uniqueUrl += "?_t=" + System.DateTime.Now.Ticks;
            }

            Debug.Log($"正在打开URL: {uniqueUrl}");
            this.GetSystem<IGameSystem>().OpenUrl(uniqueUrl);
        }
    }
}