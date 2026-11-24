using UnityEngine;
using System.Collections.Generic;

public class EnemyDetection : MonoBehaviour
{
    private List<GameObject> validTargets;
    public GameObject currentTarget { get; private set; }
    private GameObject player;
    private EnemyHealth health;
    private bool isShot = false;
    private bool heardShot;
    [SerializeField] private float viewDistance = 10f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float playerRange = 8f;
    [SerializeField] private float shotHearingRange = 10f;
    [SerializeField] private LayerMask wall;

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
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
        validTargets = new List<GameObject>(GameObject.FindGameObjectsWithTag("Good"));

        player = GameObject.Find("Player");

        if (!validTargets.Contains(player))
        {
            validTargets.Add(player);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, playerRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shotHearingRange);
    }

    void OnPlayerShot(Vector3 shotPosition)
    {
        if (Vector3.Distance(transform.position, shotPosition) > shotHearingRange) return;

        heardShot = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;
        if (health != null && health.isDead)
        {
            currentTarget = null;
            return;
        }
        DetectTargets();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            isShot = true;
        }
    }

    void DetectTargets()
    {
        GameObject observedPlayer = null;

        if (currentTarget == player)
        {
            observedPlayer = player;
            currentTarget = null;
        }
        else if (currentTarget == null || !CanSeeTarget(currentTarget.transform))
        {
            currentTarget = null;
        }

        for (int i = 0; i < validTargets.Count; i++)
        {
            if (validTargets[i] == null)
                continue;

            float distance = Vector3.Distance(validTargets[i].transform.position, transform.position);
            if (CanSeeTarget(validTargets[i].transform))
            {
                if (currentTarget == null || distance < Vector3.Distance(currentTarget.transform.position, transform.position))
                {
                    currentTarget = validTargets[i];
                }
            }
        }

        if (currentTarget == null)
            currentTarget = observedPlayer;

        if (isShot || heardShot || (CanSeeTarget(player.transform) && Vector3.Distance(player.transform.position, transform.position) < playerRange))
        {
            currentTarget = player;
            isShot = false;
            heardShot = false;
        }
    }

    public bool CanSeeTarget(Transform target)
    {
        if (target == null) return false;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget > viewDistance) return false;


        Vector3 dirToTarget = (target.position - transform.position).normalized;
        float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

        if (angleToTarget < viewAngle * 0.5f)
        {

            if (!Physics.Raycast(transform.position, dirToTarget, distanceToTarget, wall)) return true;
        }
        return false;
    }
}
