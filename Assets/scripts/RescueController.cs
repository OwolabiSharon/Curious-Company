using UnityEngine;
using UnityEngine.AI;

public class RescueController : MonoBehaviour
{
    GameObject player;
    Vector3 destination;
    NavMeshAgent nma;
    public bool isFollowing = true;
    public Transform[] hidingSpots;
    public Transform zombieGate;
    int sidewaysDirection;
    public int minRange = 10;
    public int maxRange = 15;
    public int tagDistance = 1;
    public int maxSpooks = 1;
    int spooks = 0;
    int backwardDistance;
    bool isParasite = false;

    public float turnRange;
    bool isWalkingToGate = false;
    public TakeDamage damage;
    public GameObject healthUI;
    public GameManager gm;
    public Animator anim;
    public Transform gate;
    CapsuleCollider cc;
    Rigidbody rb;
    public bool isDead = false;
    public bool IsRescued { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nma = GetComponent<NavMeshAgent>();
        player = GameObject.Find("Player");
        gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CapsuleCollider>();
        healthUI.SetActive(isFollowing);
        RandomVals();
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead || IsRescued) return;
        if (nma.isOnNavMesh) nma.isStopped = !gm.isPlaying;
        if (!gm.isPlaying)
        {
            anim.SetBool("isMoving", false);
            return;
        }
        if (damage.health < 1)
        {
            healthUI.SetActive(false);
            isDead = true;
            if (nma.isOnNavMesh) nma.isStopped = true;
            anim.Play("Death");
            gm.GameOver();
            cc.enabled = false;
            Destroy(rb);
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

        if (!isFollowing)
        {
            if (Vector3.Distance(player.transform.position, transform.position) < tagDistance)
            {
                isFollowing = true;
                anim.SetBool("isFollowing", true);
                healthUI.SetActive(true);
            }
            else
            {
                return;
            }
        }


        // if (gm.infectedTurned && (spooks < maxSpooks))
        // {
        //     Spooked();
        //     return;
        // }

        Follow();

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Safe") && gm.isPlaying && !IsRescued && !isDead && isFollowing)
        {
            IsRescued = true;
            isFollowing = false;
            if (nma.isOnNavMesh) nma.ResetPath();
            anim.SetBool("isMoving", false);
            anim.SetBool("isFollowing", false);
            healthUI.SetActive(false);
            damage.enabled = false;
            cc.enabled = false;
            if (rb) rb.isKinematic = true;
            gameObject.tag = "Untagged";
            gm.rescued += 1;
        }
    }

    void Follow()
    {
        if (Vector3.Distance(player.transform.position, transform.position) < maxRange) return;
        destination = player.transform.position - (player.transform.forward * backwardDistance) + (player.transform.right * sidewaysDirection * backwardDistance);
        // Vector3 destination = new Vector3(player.transform.x - "a little to the side", 0, player.transform.z - "how far back");
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 2f, NavMesh.AllAreas))
        {

            nma.SetDestination(hit.position);
        }
    }

    void RandomVals()
    {
        sidewaysDirection = Random.Range(-1, 2);
        backwardDistance = Random.Range(minRange, maxRange);
    }

    void Spooked()
    {
        isFollowing = false;
        healthUI.SetActive(false);
        anim.SetBool("isFollowing", false);
        anim.Play("Running Away");
        spooks += 1;
        int index = Random.Range(0, hidingSpots.Length);
        nma.SetDestination(hidingSpots[index].position);
    }

    void Infected()
    {
        if (Vector3.Distance(zombieGate.position, transform.position) < turnRange && !gm.infectedTurned)
        {
            isWalkingToGate = true;
            gm.infectedTurned = true;
            nma.SetDestination(zombieGate.position);
        }

        if (Vector3.Distance(zombieGate.position, transform.position) < tagDistance && isWalkingToGate)
        {
            //gate open
            gate.rotation = Quaternion.Euler(0f, 90f, 0f);
            gm.isFree = true;
        }
    }
}
