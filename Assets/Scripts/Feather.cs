using System.Collections;
using UnityEngine;

public class Feather : MonoBehaviour
{
    public float moveSpeed = 2f; // 向上移动的速度
    public float destroyTime = 0.2f; // 销毁时间，可在 Inspector 中设置

    // Memory optimization: use timer instead of coroutine + WaitForSeconds allocation
    private float _timer;

    private void OnEnable()
    {
        _timer = 0f;
    }

    private void Update()
    {
        // 每帧向上移动
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        _timer += Time.deltaTime;
        if (_timer >= destroyTime)
        {
            Destroy(gameObject);
        }
    }
}
