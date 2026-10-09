using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float bulletSpeed = 40;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, 3);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * bulletSpeed * Time.deltaTime);
        RaycastHit hit;
        Vector3 frameMovement = transform.forward * bulletSpeed * Time.deltaTime;
        float frameDistance = frameMovement.magnitude;
        if (Physics.Raycast(transform.position, transform.forward, out hit, frameDistance))
        {
            if (hit.collider.CompareTag("wall"))
            {
                Debug.Log("wall");
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Good")) return;
        Destroy(gameObject);
    }
}
