using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float bulletSpeed = 40;
    bool spent;

    void Start()
    {
        Destroy(gameObject, 3);
    }

    void Update()
    {
        if (spent) return;
        if (GameManager.Instance != null && !GameManager.Instance.isPlaying)
        {
            Destroy(gameObject);
            return;
        }
        float distance = bulletSpeed * Time.deltaTime;
        // Sweep the distance travelled this frame, so thin walls and targets cannot be skipped.
        var hits = Physics.RaycastAll(transform.position, transform.forward, distance, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (Hit(hit.collider, hit.point)) return;
        }
        transform.position += transform.forward * distance;
    }

    bool Hit(Collider other, Vector3 position)
    {
        if (spent || other.CompareTag("Good") || other.CompareTag("Bullet") || other.CompareTag("Safe")) return false;
        // Friendly child colliders and cosmetic triggers do not consume a shot.
        if (other.GetComponentInParent<TakeDamage>() != null) return false;
        var enemy = other.GetComponentInParent<EnemyController>();
        if (other.isTrigger && enemy == null) return false;
        spent = true;
        if (enemy != null) enemy.ReceiveBullet(position);
        Destroy(gameObject);
        return true;
    }

    void OnTriggerEnter(Collider other)
    {
        Hit(other, transform.position);
    }
}
