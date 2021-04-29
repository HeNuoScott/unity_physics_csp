using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collision : MonoBehaviour
{
    //物理世界不同 不能产生碰撞
    private void OnCollisionEnter(UnityEngine.Collision collision)
    {
        Debug.Log(collision.collider.name);
    }
}
