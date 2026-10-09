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
        if (health < 1 || GameManager.Instance == null || !GameManager.Instance.isPlaying) return;
        if (other.CompareTag("Enemy") && canTakeDamage)
        {
            health -= CampaignDirector.Instance != null ? CampaignSession.IncomingDamage : 1f;
            canTakeDamage = false;
            Invoke("SetBool", iFrameDur);
            if (rb != null && !rb.isKinematic) rb.AddForce(other.transform.forward * attackKnockBack, ForceMode.Impulse);
            anim.CrossFadeInFixedTime("React", .08f);
            if (hp) hp.fillAmount = health / maxHealth;
        }
    }

    void SetBool()
    {
        canTakeDamage = true;
    }
}
