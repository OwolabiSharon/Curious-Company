using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;


public class EnemyController : MonoBehaviour
{
    NavMeshAgent nma;
    public GameObject[] objects;
    public GameObject closestWR = null;
    public GameObject Player;
    public List<GameObject> objectsWithinRange = new List<GameObject>();
    public float range = 10f;
    [Range(0, 360)] public float viewAngle = 90f;
    public float playerRange = 8f;
    public float shotHearingRange = 10f;
    public float health = 5;
    public float maxHealth = 5;
    public bool isShot = false;
    public Animator anim;
    public bool isCaged = false;
    public GameManager gm;
    public ParticleSystem part;
    public Image healthBar;
    public GameObject healthUI;
    private AudioSource audioSource;
    public AudioClip growl;
    public LayerMask layer;
    public bool isDead = false;
    CapsuleCollider cc;
    bool heardShot;

    void Awake()
    {
        nma = GetComponent<NavMeshAgent>();

    }

    void OnEnable()
    {
        CharacterController.ShotFired += OnPlayerShot;
    }

    void OnDisable()
    {
        CharacterController.ShotFired -= OnPlayerShot;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        objects = GameObject.FindGameObjectsWithTag("Good");
        Player = GameObject.Find("Player");
        gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        audioSource = GetComponent<AudioSource>();
        cc = GetComponent<CapsuleCollider>();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, playerRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shotHearingRange);
    }

    void OnPlayerShot(Vector3 shotPosition)
    {
        Debug.Log(isDead);
        Debug.Log(Vector3.Distance(transform.position, shotPosition));
        if (isDead || Vector3.Distance(transform.position, shotPosition) > shotHearingRange) return;

        heardShot = true;
        nma.SetDestination(shotPosition);
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead) return;
        if (health < 1f)
        {
            anim.Play("Death");
            isDead = true;
            nma.isStopped = true;
            cc.enabled = false;
            Destroy(gameObject, 2);
            return;
        }
        if (isCaged && !gm.isFree) return;
        if (nma.hasPath && !nma.pathPending && nma.remainingDistance > nma.stoppingDistance)
        {
            anim.SetBool("isMoving", true);
        }
        else
        {
            anim.SetBool("isMoving", false);
        }
        if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Emote") && !anim.IsInTransition(0))
        {
            nma.isStopped = true;
        }
        else
        {
            nma.isStopped = false;
        }
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
                continue;

            float distance = Vector3.Distance(objects[i].transform.position, transform.position);
            if (CanSeeTarget(objects[i].transform))
            {
                if (closestWR == null || distance < Vector3.Distance(closestWR.transform.position, transform.position))
                {
                    closestWR = objects[i];
                }

            }
        }
        if (isShot || heardShot || (CanSeeTarget(Player.transform) && Vector3.Distance(Player.transform.position, transform.position) < playerRange))

        {
            healthUI.SetActive(true);
            nma.destination = Player.transform.position;
            isShot = false;
            // audioSource.PlayOneShot(growl);
            return;
        }
        if (closestWR == null) return;


        nma.destination = closestWR.transform.position;
        healthUI.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        if (other.CompareTag("Bullet"))
        {
            health -= 1f;
            isShot = true;
            anim.Play("React");
            ParticleSystem blood = Instantiate(part, other.transform.position, part.transform.rotation);
            blood.Play();
            healthBar.fillAmount = health / maxHealth;
        }
        else if (other.CompareTag("Good"))
        {
            anim.Play("Attack");
        }
    }

    public bool CanSeeTarget(Transform target)
    {
        if (target == null) return false;

        // 1. Check if the target is within the maximum viewing distance
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget > range)
        {
            return false;
        }

        // 2. Calculate the direction to the target and the angle between forward and target
        Vector3 dirToTarget = (target.position - transform.position).normalized;
        float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);
        // 3. Check if the angle falls within half of our total field of view cone
        if (angleToTarget < 45)
        {
            // 4. Fire a raycast to see if a wall blocks the view
            if (!Physics.Raycast(transform.position, dirToTarget, distanceToTarget, layer))
            {
                return true; // No wall hit! The target is visible.
            }
        }
        return false; // Target is either outside the cone or behind a wall
    }
}
