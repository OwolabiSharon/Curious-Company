using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RescueController : MonoBehaviour
{
    GameObject player;
    Vector3 destination;
    static readonly List<RescueController> formation = new List<RescueController>();
    AgentMotion motion;
    Rigidbody playerBody;
    Vector3 followHeading = Vector3.forward;
    int formationSlot = -1;
    float nextFollow;
    [Min(1f)] public float movementAcceleration = 10f;
    [Min(.02f)] public float turnSmoothTime = .10f;
    [Min(90f)] public float turnSpeed = 600f;
    public float referenceRunSpeed = 4.2f;
    NavMeshAgent nma;
    public bool isFollowing = true;
    public Transform[] hidingSpots;
    public Transform zombieGate;
    public int minRange = 10;
    public int maxRange = 15;
    public int tagDistance = 1;
    public int maxSpooks = 1;
    int spooks = 0;
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
        motion = new AgentMotion(nma, anim, rb, movementAcceleration);
        playerBody = player.GetComponent<Rigidbody>();
        followHeading = player.transform.forward;
        if (isFollowing) Recruit();
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead || IsRescued) return;
        if (!gm.isPlaying)
        {
            motion.Stop();
            motion.Tick(turnSmoothTime, turnSpeed, referenceRunSpeed);
            return;
        }
        if (damage.health < 1)
        {
            healthUI.SetActive(false);
            isDead = true;
            if (nma.isOnNavMesh) nma.isStopped = true;
            anim.CrossFadeInFixedTime("Death", .12f);
            ReleaseSlot();
            gm.GameOver();
            cc.enabled = false;
            Destroy(rb);
            return;
        }

        if (!nma.isOnNavMesh) return;
        motion.Tick(turnSmoothTime, turnSpeed, referenceRunSpeed);
        if (!isFollowing)
        {
            if (Vector3.Distance(player.transform.position, transform.position) < tagDistance)
            {
                Recruit();
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

        var state = anim.GetCurrentAnimatorStateInfo(0);
        bool reacting = state.IsName("React") && !anim.IsInTransition(0);
        if (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsName("React")) reacting = true;
        if (reacting) motion.Stop();
        else
        {
            motion.Resume();
            Follow();
        }

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Safe") && gm.isPlaying && !IsRescued && !isDead && isFollowing)
        {
            IsRescued = true;
            ReleaseSlot();
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

    void Recruit()
    {
        isFollowing = true;
        anim.SetBool("isFollowing", true);
        healthUI.SetActive(true);
        if (formationSlot >= 0) return;
        formationSlot = formation.FindIndex(member => member == null);
        if (formationSlot < 0)
        {
            formationSlot = formation.Count;
            formation.Add(this);
        }
        else formation[formationSlot] = this;
        gm.totalFollowing++;
        nextFollow = 0;
    }

    void ReleaseSlot()
    {
        if (formationSlot < 0) return;
        if (formationSlot < formation.Count && formation[formationSlot] == this) formation[formationSlot] = null;
        formationSlot = -1;
        if (gm) gm.totalFollowing = Mathf.Max(0, gm.totalFollowing - 1);
    }

    void OnDisable()
    {
        ReleaseSlot();
    }

    void Follow()
    {
        if (formationSlot < 0) Recruit();
        Vector3 velocity = playerBody ? playerBody.linearVelocity : Vector3.zero;
        velocity.y = 0;
        // Mouse aiming does not rotate the escort formation. Remember the last walking direction at rest.
        if (velocity.sqrMagnitude > .16f)
        {
            float current = Mathf.Atan2(followHeading.x, followHeading.z) * Mathf.Rad2Deg;
            float target = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
            float yaw = Mathf.LerpAngle(current, target, 1f - Mathf.Exp(-5f * Time.deltaTime));
            followHeading = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        }
        if (Time.time < nextFollow) return;
        nextFollow = Time.time + .20f;
        int row = formationSlot / 2;
        float side = formationSlot % 2 == 0 ? -.65f : .65f;
        Vector3 right = Vector3.Cross(Vector3.up, followHeading);
        destination = player.transform.position - followHeading * (Mathf.Max(1f, minRange) + row * .75f) + right * side;
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 1.2f, nma.areaMask) && motion.CanReach(hit.position))
            motion.SetDestination(hit.position);
        // At narrow doors, follow the player's reachable route instead of insisting on a blocked formation slot.
        else if (NavMesh.SamplePosition(player.transform.position, out hit, 2f, nma.areaMask) && motion.CanReach(hit.position))
            motion.SetDestination(hit.position);
    }

    void Spooked()
    {
        isFollowing = false;
        ReleaseSlot();
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
