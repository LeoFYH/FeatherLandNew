using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Egg : MonoBehaviour
{
    public GameObject[] bridPre; // 可能生成的鸟的预制体
    public Sprite[] eggSprites; // 蛋动画的每一帧图片
    public SpriteRenderer spriteRenderer;
    private int currentFrame = 0; // 当前显示的帧索引

    private void Start()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer not found on the Egg object!");
        }
    }

    private void OnMouseDown()
    {
        spriteRenderer.transform.DOShakeScale(0.2f, 0.5f, 50, 180f);
        //spriteRenderer.transform.DOShakePosition(0.2f, 0.5f);
        PlayNextFrame();
        
        if (currentFrame >= eggSprites.Length)
        {
            SpawnBird();
        }
    }

    private void PlayNextFrame()
    {
        if (currentFrame < eggSprites.Length)
        {
            spriteRenderer.sprite = eggSprites[currentFrame];
            currentFrame++;
        }
    }

    private void SpawnBird()
    {
        // 随机选择鸟的预制体
        int val = Random.Range(0, bridPre.Length);
        GameObject go = Instantiate(bridPre[val]);

        // 查找WalkableArea
        WalkableArea walkableArea = NavigationManager.Instance.GetWalkableArea(3);
        
        if (walkableArea != null)
        {
            // 在可行走区域内获取随机点
            Vector2 randomPoint = walkableArea.GetRandomPointInArea();
            go.transform.position = new Vector3(randomPoint.x, randomPoint.y, 0);
        }
        else
        {
            // 如果没有找到WalkableArea，使用原来的位置逻辑
            go.transform.position = new Vector3(transform.position.x, -4, transform.position.z);
        }

        // 更新 GameManager 的未开启蛋数量
        GameManager.Instance.noOpenEggs--;

        // 销毁当前蛋对象
        Destroy(gameObject);
    }
}
