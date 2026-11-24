using UnityEngine;
using UnityEngine.AI;


public class EnemyCombat : MonoBehaviour
{
    private NavMeshAgent nma;
    private EnemyDetection detection;
    private EnemyHealth health;
    private GameManager gm;
    public Animator anim;
    public GameObject healthUI;
    [Tooltip("Caged zombies stay put until the gate is opened (GameManager.isFree).")]
    public bool isCaged = false;
    private float triggerTimer = 0f;
    [SerializeField] private float attackDelay = 1f;
    private bool stopped;

    void Awake()
    {
        nma = GetComponent<NavMeshAgent>();
        detection = GetComponent<EnemyDetection>();
        health = GetComponent<EnemyHealth>();

    }

    void Start()
    {
        gm = GameManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (health != null && health.isDead)
        {
            StopMoving();
            return;
        }
        if (isCaged && (gm == null || !gm.isFree)) return;
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
        if (detection.currentTarget == null) return;


        nma.SetDestination(detection.currentTarget.transform.position);
        healthUI.SetActive(true);
    }

    // a dead zombie stops where it fell
    void StopMoving()
    {
        if (stopped) return;
        stopped = true;
        anim.SetBool("isMoving", false);
        if (nma.isOnNavMesh)
        {
            nma.isStopped = true;
            nma.ResetPath();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (health != null && health.isDead) return;

        if (other.CompareTag("Good"))
        {
            if (other.gameObject == gameObject) return;
            triggerTimer += Time.deltaTime;

            if (triggerTimer >= attackDelay)
            {
                Attack();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Good"))
        {
            triggerTimer = 0f;
        }
    }
    void Attack()
    {
        anim.Play("Attack");
        triggerTimer = 0f;
    }
}
