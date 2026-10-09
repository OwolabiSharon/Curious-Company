using UnityEngine;
using UnityEngine.UI;

public class TakeDamage : MonoBehaviour
{
    bool canTakeDamage = true;
    public float health = 8;
    public float maxHealth = 8;
    public float iFrameDur = 3;
    Rigidbody rb;
    public Animator anim;
    public Image hp;


    public float attackKnockBack = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        if (health < 1) return;
        if (other.CompareTag("Enemy") && canTakeDamage)
        {
            health -= 1;
            canTakeDamage = false;
            Invoke("SetBool", iFrameDur);
            rb.AddForce(other.transform.forward * attackKnockBack, ForceMode.Impulse);
            anim.Play("React");
            hp.fillAmount = health / maxHealth;
        }
    }

    void SetBool()
    {
        canTakeDamage = true;
    }
}
