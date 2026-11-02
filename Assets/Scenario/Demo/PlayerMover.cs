using UnityEngine;
using UnityEngine.InputSystem;

/// Движение игрока по WASD для демонстрационной сцены.
[RequireComponent(typeof(Rigidbody))]
public class PlayerMover : MonoBehaviour
{
    public float speed = 5f;

    Rigidbody body;

    void Awake() => body = GetComponent<Rigidbody>();

    void FixedUpdate()
    {
        var k = Keyboard.current;
        if (k == null) return;
        var dir = new Vector3((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), 0, (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
        var v = Vector3.ClampMagnitude(dir, 1) * speed;
        body.linearVelocity = new Vector3(v.x, body.linearVelocity.y, v.z);
    }
}
