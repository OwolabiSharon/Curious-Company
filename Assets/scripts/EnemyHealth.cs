using UnityEngine;
using UnityEngine.UI;


public class EnemyHealth : MonoBehaviour
{

    private GameObject player;
    private float currentHealth;
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float hitDamage = 1f;

    [SerializeField] private Animator anim;

    [SerializeField] private ParticleSystem bloodParticle;
    [SerializeField] private Image healthBar;
    [SerializeField] private GameObject healthUI;
    public bool isDead { get; private set; } = false;
    private CapsuleCollider capsuleCollider;
    private float damageCooldownTimer = 1f;
    private float damageDelay = 1f;

    private Rigidbody rb;

    void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        //animator gets inputed from the inspector
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    void Update()
    {
        if (damageCooldownTimer < damageDelay)
        {
            damageCooldownTimer += Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        if (other.CompareTag("Bullet"))
        {
            if (damageCooldownTimer >= damageDelay)
            {
                TakeDamage(hitDamage, other.transform.position);
            }

        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        if (anim != null)
            anim.Play("Death");

        if (capsuleCollider != null)
            capsuleCollider.enabled = false;

        if (healthUI != null)
            healthUI.SetActive(false);

        if (rb != null)
            Destroy(rb);

        Destroy(gameObject, 2f);
    }

    private void TakeDamage(float damage, Vector3 hitPosition)
    {
        damageCooldownTimer = 0f;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        if (anim != null)
            anim.Play("React");

        if (bloodParticle != null)
        {
            ParticleSystem blood = Instantiate(bloodParticle, hitPosition, bloodParticle.transform.rotation);
            blood.Play();
            Destroy(blood.gameObject, blood.main.duration + blood.main.startLifetime.constantMax);
        }

        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

}
