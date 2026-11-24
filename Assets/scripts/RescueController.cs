using UnityEngine;
using UnityEngine.AI;

public class RescueController : MonoBehaviour
{
    GameObject player;
    Vector3 destination;
    NavMeshAgent nma;
    private bool isFollowing = false;
    private bool isSafe = false; // reached the safe zone; can't be tagged or counted again
    private int sidewaysDirection;
    [SerializeField] private int minRange = 10;
    [SerializeField] private int maxRange = 15;
    [SerializeField] public int tagDistance = 1;
    [SerializeField] private GameObject healthUI;
    [SerializeField] public bool isDead = false;
    private int backwardDistance;
    [SerializeField] private TakeDamage damage;
    private GameManager gm;
    [SerializeField] private Animator anim;
    private CapsuleCollider cc;
    private Rigidbody rb;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nma = GetComponent<NavMeshAgent>();
        player = GameObject.Find("Player");
        gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CapsuleCollider>();
        healthUI.SetActive(isFollowing);
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead) return;
        if (damage.health < 1)
        {
            Death();
            return;
        }
        if (nma.velocity.magnitude > 0f)
        {
            anim.SetBool("isMoving", true);
        }
        else
        {
            anim.SetBool("isMoving", false);
        }
        if (isSafe) return;
        if (!isFollowing)
        {
            CheckTag();
            return;
        }
        Follow();
    }

    void CheckTag()
    {
        if (Vector3.Distance(player.transform.position, transform.position) < tagDistance)
        {
            isFollowing = true;
            anim.SetBool("isFollowing", true);
            healthUI.SetActive(true);
        }
    }

    void Death()
    {
        healthUI.SetActive(false);
        isDead = true;
        anim.Play("Death");
        gm.GameOver();
        cc.enabled = false;
        Destroy(rb);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Safe") && isFollowing && !isSafe)
        {
            gm.rescued += 1;
            isFollowing = false;
            isSafe = true;
            anim.SetBool("isFollowing", false);
            nma.ResetPath();
        }
    }

    void Follow()
    {
        RandomFollowVals();
        if (Vector3.Distance(player.transform.position, transform.position) < maxRange) return; // so it moves only after the player as gone a certain distance from it (reusing the maxrange variable)
        destination = player.transform.position - (player.transform.forward * backwardDistance) + (player.transform.right * sidewaysDirection * backwardDistance);
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 2f, NavMesh.AllAreas))
        {

            nma.SetDestination(hit.position);
        }
    }

    void RandomFollowVals()
    {
        sidewaysDirection = Random.Range(-1, 2); // 2 because max is excluded in the computation
        backwardDistance = Random.Range(minRange, maxRange);
    }
}
