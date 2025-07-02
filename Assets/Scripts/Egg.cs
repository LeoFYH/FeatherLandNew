using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Egg : MonoBehaviour
{
    public GameObject[] bridPre; // 可能生成的鸟的预制体
    public Sprite[] eggSprites; // 蛋动画的每一帧图片
    public SpriteRenderer spriteRenderer;
    private int currentFrame = 0; // 当前显示的帧索引
    private Tweener anim;

    private void Start()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer not found on the Egg object!");
        }
    }

    private void OnMouseDown()
    {
        anim?.Kill();
        anim = spriteRenderer.transform.DOShakeScale(0.2f, 0.5f, 50, 180f);
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

    private void OnDestroy()
    {
        anim?.Kill();
    }

    private void SpawnBird()
    {
        // 随机选择鸟的预制体
        int val = Random.Range(0, bridPre.Length);
        GameObject go = Instantiate(bridPre[val]);
        var agent = go.GetComponent<NavMeshAgent>();
        agent.enabled = false;

        var point = NavigationManager.Instance.GetRandomTarget(3);
        go.transform.position = new Vector3(point.x, point.y, 0);
        // 更新 GameManager 的未开启蛋数量
        GameManager.Instance.noOpenEggs--;

        agent.enabled = true;
        // 销毁当前蛋对象
        Destroy(gameObject);
    }
}
