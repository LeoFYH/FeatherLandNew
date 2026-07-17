using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    /// <summary>
    /// 制作人员名单滚动展示
    /// 将本脚本挂载到 Credits 面板的根节点，并把人员名单内容的 RectTransform 拖到 content 字段即可。
    /// 内容会从当前位置开始向上滚动，直到完全滚出可视区域。
    /// </summary>
    public class CreditsPopup : UIBase
    {
        [Header("内容设置")]
        [Tooltip("滚动内容的 RectTransform（所有制作人员名单条目的父节点）")]
        public RectTransform content;

        [Tooltip("滚动速度（像素/秒）")]
        public float scrollSpeed = 80f;

        [Tooltip("是否循环滚动")]
        public bool loop = false;

        [Tooltip("每次循环之间的间隔（秒）")]
        public float loopInterval = 1f;

        [Header("状态")]
        [SerializeField]
        private bool isPlaying = false;

        public Button closeButton;

        /// <summary>
        /// 滚动完成时触发（非循环模式下仅触发一次）
        /// </summary>
        public Action OnComplete;

        private Vector2 initialAnchoredPosition;
        private float loopDelayTimer;

        protected override void Awake()
        {
            base.Awake();

            if (content != null)
            {
                initialAnchoredPosition = content.anchoredPosition;
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() =>
                {
                    this.GetSystem<IUISystem>().HidePopup(UIPopup.CreditsPopup);
                });
            }
            else
            {
                Debug.LogWarning("[CreditsPopup] closeButton 未赋值，无法通过点击关闭");
            }
        }

        private void OnEnable()
        {
            if (isPlaying)
            {
                Play();
            }
        }

        private void Update()
        {
            if (!isPlaying || content == null) return;

            // 处理循环间隔
            if (loopDelayTimer > 0f)
            {
                loopDelayTimer -= Time.deltaTime;
                return;
            }

            Vector2 pos = content.anchoredPosition;
            pos.y += scrollSpeed * Time.deltaTime;
            content.anchoredPosition = pos;

            if (HasCompletelyScrolledOff())
            {
                if (loop)
                {
                    ResetToStart();
                    loopDelayTimer = loopInterval;
                }
                else
                {
                    Pause();
                    this.GetSystem<IUISystem>().HidePopup(UIPopup.CreditsPopup);
                }
            }
        }

        /// <summary>
        /// 开始滚动
        /// </summary>
        public void Play()
        {
            if (content == null)
            {
                Debug.LogError("[CreditsRoll] content 未赋值，无法滚动", this);
                return;
            }

            isPlaying = true;
        }

        /// <summary>
        /// 暂停滚动（保留当前位置）
        /// </summary>
        public void Pause()
        {
            isPlaying = false;
        }

        /// <summary>
        /// 停止滚动并回到起始位置
        /// </summary>
        public void Stop()
        {
            Pause();
            ResetToStart();
        }

        /// <summary>
        /// 从起始位置重新开始滚动
        /// </summary>
        public void Restart()
        {
            ResetToStart();
            Play();
        }

        /// <summary>
        /// 重置内容到初始位置
        /// </summary>
        public void ResetToStart()
        {
            if (content == null) return;

            content.anchoredPosition = initialAnchoredPosition;
            loopDelayTimer = 0f;
        }

        /// <summary>
        /// 判断内容是否已完全滚出可视区域
        /// </summary>
        private bool HasCompletelyScrolledOff()
        {
            RectTransform viewport = content.parent as RectTransform;
            if (viewport == null) return false;

            Vector3[] contentCorners = new Vector3[4];
            Vector3[] viewportCorners = new Vector3[4];
            content.GetWorldCorners(contentCorners);
            viewport.GetWorldCorners(viewportCorners);

            float contentBottom = Mathf.Min(contentCorners[0].y, contentCorners[3].y);
            float viewportTop = Mathf.Max(viewportCorners[1].y, viewportCorners[2].y);

            return contentBottom >= viewportTop;
        }

        private void OnValidate()
        {
            if (scrollSpeed < 0f) scrollSpeed = 0f;
            if (loopInterval < 0f) loopInterval = 0f;
        }
    }
}
