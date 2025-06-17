using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Feather : MonoBehaviour
{
    public float moveSpeed = 2f; // 向上移动的速度
    public float destroyTime = 0.2f; // 销毁时间，可在 Inspector 中设置

    private void Start()
    {
        // 启动销毁协程
        StartCoroutine(DestroyAfterDelay());
    }

    private void Update()
    {
        // 每帧向上移动
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }

    private IEnumerator DestroyAfterDelay()
    {
        // 等待 destroyTime 秒
        yield return new WaitForSeconds(destroyTime);

        // 销毁当前对象
        Destroy(gameObject);
    }
}
