using System.Collections;
using DG.Tweening;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BirdGame
{
    public class Egg : ViewControllerBase
    {
        [ShowInInspector, ReadOnly]
        public int EggItemIndex { get; private set; }
        public int BirdIndex { get; private set; }
        
        public SpriteRenderer spriteRenderer;
        public GameObject effect1;
        public GameObject effect2;
        public Sprite[] eggSps;
        private int currentFrame = 0; // 当前显示的帧索引
        private Tweener anim;
        private Tweener floatAnim; // 浮动动画
        private bool isSetBird;
        private int eggId;

        public void SetEggIndex(int index, int id)
        {
            EggItemIndex = index;
            eggId = id;
            isSetBird = false;
            spriteRenderer.sprite = eggSps[this.GetModel<IGameModel>().ShopEggSelectIndex.Value];
        }

        public void SetBirdIndex(int index)
        {
            BirdIndex = index;
            eggId = -1;
            isSetBird = true;
            spriteRenderer.sprite = eggSps[0];
        }

        /// <summary>
        /// 开始缓慢的上下浮动动画
        /// </summary>
        private void StartFloatingAnimation()
        {
            // 停止之前的浮动动画
            floatAnim?.Kill();
            
            // 为每个蛋创建轻微不同的动画参数
            float baseDuration = 2f;
            float durationVariation = Random.Range(-0.3f, 0.3f); // 轻微变化动画时长
            float finalDuration = baseDuration + durationVariation;
            
            // 根据蛋的索引创建轻微的相位差
            float phaseOffset = EggItemIndex * 0.3f; // 每个蛋偏移0.3秒的相位
            
            // 创建缓慢的上下浮动动画
            floatAnim = transform.DOMoveY(transform.position.y + 0.3f, finalDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo) // 无限循环，来回摆动
                .SetDelay(phaseOffset); // 设置轻微的延迟
        }

        private void Start()
        {
            // CPU优化：注册到EggList，避免FindGameObjectsWithTag("Egg")
            this.GetModel<IBirdModel>().EggList.Add(this);

            this.RegisterEvent<DestroyEggEvent>(evt =>
            {
                Destroy(gameObject);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            if (spriteRenderer == null)
            {
                Debug.LogError("SpriteRenderer not found on the Egg object!");
            }
            effect1.SetActive(true);
            effect2.SetActive(false);

            // 开始缓慢的上下浮动动画
            StartFloatingAnimation();

            this.RegisterEvent<HideEggEvent>(evt =>
            {
                gameObject.SetActive(false);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<ShowEggEvent>(evt =>
            {
                gameObject.SetActive(true);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        public void OnClick()
        {
            if(this.GetModel<IGameModel>().IsSettingOpen)
                return;
            
            this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Hatch);
            SpawnBird();

            // PlayNextFrame();
            //
            // if (currentFrame >= eggSprites.Length)
            // {
            //     SpawnBird();
            // }
        }

        private void OnDestroy()
        {
            // CPU优化：从EggList注销
            this.GetModel<IBirdModel>().EggList.Remove(this);

            anim?.Kill();
            floatAnim?.Kill(); // 停止浮动动画
            
            // 停止所有粒子特效
            if (effect1 != null)
            {
                var particleSystem1 = effect1.GetComponent<ParticleSystem>();
                if (particleSystem1 != null)
                {
                    particleSystem1.Stop();
                }
            }
            
            if (effect2 != null)
            {
                var particleSystem2 = effect2.GetComponent<ParticleSystem>();
                if (particleSystem2 != null)
                {
                    particleSystem2.Stop();
                }
            }
        }

        private void SpawnBird()
        {
            if(isSetBird)
                this.SendCommand(new SpawnBirdCommand(BirdIndex, isSetBird));
            else
                this.SendCommand(new SpawnBirdCommand(EggItemIndex, isSetBird));
            this.GetModel<IGameModel>().OpenEggIndex = -1;
            // 销毁当前蛋对象
            Destroy(gameObject);
        }
    }
}