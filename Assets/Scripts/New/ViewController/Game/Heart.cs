using UnityEngine;
using QFramework;

namespace BirdGame
{
    public class Heart : ViewControllerBase
    {
        public float lifetime = 0.5f; // 心形特效存活时间
        private float timer = 0f;
        private bool isRecycling = false;
        private SpriteRenderer[] heartSr;
        private SpriteRenderer birdSr;

        private void OnEnable()
        {
            // 每次从对象池中取出时重置
            timer = 0f;
            isRecycling = false;
            
            // 获取心形特效的SpriteRenderer
            if (heartSr == null)
            {
                heartSr = GetComponentsInChildren<SpriteRenderer>();
            }
            transform.localRotation = Quaternion.identity;
            // 获取鸟的SpriteRenderer（从父物体或父物体的父物体）
            if (birdSr == null)
            {
                if (transform.parent != null)
                {
                    // 先尝试从父物体的父物体（鸟）获取
                    birdSr = transform.parent.GetComponentInParent<SpriteRenderer>();
                    if (birdSr == null)
                    {
                        // 如果找不到，从父物体获取
                        birdSr = transform.parent.GetComponentInChildren<SpriteRenderer>();
                    }
                }
            }
        }

        private void Update()
        {
            // 同步鸟的翻转状态
            if (heartSr != null && birdSr != null)
            {
                for(int i=0;i<heartSr.Length;i++)
                {
                    heartSr[i].flipX = !birdSr.flipX;
                }
                float x = heartSr[0].transform.localPosition.x;
                float y = heartSr[0].transform.localPosition.y;
                heartSr[0].transform.localPosition = birdSr.flipX ? new Vector2(x,y):new Vector2(-x,y);
            }
            
            // 计时器
            timer += Time.deltaTime;
            if (timer >= lifetime && !isRecycling)
            {
                RecycleToPool();
            }
        }

        public void OnHide()
        {
            RecycleToPool();
        }

        private void RecycleToPool()
        {
            if (isRecycling) return;
            isRecycling = true;

            // 获取PooledObject组件，判断是否来自对象池
            PooledObject pooledObj = GetComponent<PooledObject>();
            if (pooledObj != null && !string.IsNullOrEmpty(pooledObj.poolName))
            {
                // 回收到对象池
                this.GetSystem<IObjectPoolSystem>().Recycle(pooledObj.poolName, gameObject);
            }
            else
            {
                // 不是来自对象池，直接销毁
                GameObject.Destroy(gameObject);
            }
        }
    }
}