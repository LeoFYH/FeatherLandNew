using UnityEngine;
using QFramework;
using BirdGame;

public class Heart : MonoBehaviour
{
    public float destroyTime = 0.5f;
    private float timer = 0f;
    private bool isRecycling = false;
    private SpriteRenderer birdSr;

    private void OnEnable()
    {
        // 每次从对象池中取出时重置
        timer = 0f;
        isRecycling = false;
        
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

    void Update()
    {
        // 同步鸟的翻转状态 - 通过旋转180°来实现
        if (birdSr != null)
        {
            // 根据鸟的flipX状态旋转爱心
            if (birdSr.flipX)
            {
                transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
        }
        
        // 计时器
        timer += Time.deltaTime;
        if (timer >= destroyTime && !isRecycling)
        {
            RecycleToPool();
        }
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
            GameApp.Interface.GetSystem<IObjectPoolSystem>().Recycle(pooledObj.poolName, gameObject);
        }
        else
        {
            // 不是来自对象池，直接销毁
            Destroy(gameObject);
        }
    }
}
