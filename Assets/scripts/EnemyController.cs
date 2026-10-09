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
    [Min(1f)] public float movementAcceleration = 7f;
    [Min(.02f)] public float turnSmoothTime = .12f;
    [Min(90f)] public float turnSpeed = 540f;
    [Min(.05f)] public float perceptionInterval = .12f;
    public float referenceRunSpeed = 3.2f;
    AgentMotion motion;
    float nextPerception;
    float lastSeenUntil;
    Vector3 lastSeenPosition;
    bool hasGoal;
    bool heardShot;
    Vector3 lastHeardPosition;
    float heardUntil;

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
        motion = new AgentMotion(nma, anim, GetComponent<Rigidbody>(), movementAcceleration);
        nextPerception = Time.time + (Mathf.Repeat(transform.position.x * .17f + transform.position.z * .13f, 1f) * perceptionInterval);
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
        if (gm == null || !gm.isPlaying || !nma.isOnNavMesh || (isCaged && !gm.isFree)) return;
        if (isDead || Vector3.Distance(transform.position, shotPosition) > shotHearingRange) return;

        heardShot = true;
        lastHeardPosition = shotPosition;
        heardUntil = Time.time + 6f;
        nextPerception = 0;
    }

    void Update()
    {
        if (isDead || gm == null || motion == null) return;
        if (!gm.isPlaying)
        {
            motion.Stop();
            motion.Tick(turnSmoothTime, turnSpeed, referenceRunSpeed);
            return;
        }
        if (health < 1f)
        {
            anim.CrossFadeInFixedTime("Death", .12f);
            isDead = true;
            motion.Stop(true);
            cc.enabled = false;
            Destroy(gameObject, 2);
            return;
        }
        if (isCaged && !gm.isFree)
        {
            motion.Stop();
            motion.Tick(turnSmoothTime, turnSpeed, referenceRunSpeed);
            return;
        }
        if (!nma.isOnNavMesh) return;
        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        bool reacting = !anim.IsInTransition(0) && (state.IsTag("Emote") || state.IsName("Attack"));
        if (anim.IsInTransition(0))
        {
            var next = anim.GetNextAnimatorStateInfo(0);
            reacting = next.IsTag("Emote") || next.IsName("Attack");
        }
        if (reacting) motion.Stop();
        else
        {
            if (Time.time >= nextPerception)
            {
                nextPerception = Time.time + perceptionInterval;
                UpdateTarget();
            }
            if (hasGoal) motion.Resume();
        }
        motion.Tick(turnSmoothTime, turnSpeed, referenceRunSpeed);
    }

    void UpdateTarget()
    {
        GameObject best = null;
        float bestDistance = float.PositiveInfinity;
        foreach (GameObject candidate in objects)
        {
            if (!candidate || !candidate.CompareTag("Good") || !CanSeeTarget(candidate.transform)) continue;
            float distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }
        // Retain a visible target until a substantially closer one appears, avoiding left/right jitter in crowds.
        if (closestWR && closestWR.CompareTag("Good") && CanSeeTarget(closestWR.transform)
            && (closestWR.transform.position - transform.position).sqrMagnitude <= bestDistance * 1.4f)
            best = closestWR;
        if (Player && (isShot || (CanSeeTarget(Player.transform)
            && Vector3.Distance(Player.transform.position, transform.position) < playerRange))) best = Player;
        isShot = false;
        closestWR = best;
        if (best)
        {
            lastSeenPosition = best.transform.position;
            lastSeenUntil = Time.time + .8f;
            hasGoal = true;
            motion.SetDestination(lastSeenPosition);
            healthUI.SetActive(true);
        }
        else if (Time.time < lastSeenUntil)
        {
            hasGoal = true;
            motion.SetDestination(lastSeenPosition);
        }
        else if (heardShot && Time.time < heardUntil)
        {
            hasGoal = true;
            motion.SetDestination(lastHeardPosition);
        }
        else
        {
            heardShot = false;
            hasGoal = false;
            motion.Stop(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead || gm == null || !gm.isPlaying) return;
        if (other.CompareTag("Good"))
        {
            if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Attack")
                && !(anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsName("Attack")))
                anim.CrossFadeInFixedTime("Attack", .10f);
        }
    }

    public void ReceiveBullet(Vector3 hitPosition)
    {
        if (isDead || health < 1 || gm == null || !gm.isPlaying) return;
        health -= 1f;
        isShot = true;
        anim.CrossFadeInFixedTime("React", .08f);
        nextPerception = 0;
        if (part)
        {
            ParticleSystem blood = Instantiate(part, hitPosition, part.transform.rotation);
            blood.Play();
            Destroy(blood.gameObject, 4);
        }
        if (healthBar) healthBar.fillAmount = health / maxHealth;
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
        if (angleToTarget < viewAngle * 0.5f)
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
