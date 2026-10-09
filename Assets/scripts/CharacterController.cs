using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CharacterController : MonoBehaviour
{
    public static event System.Action<Vector3> ShotFired;

    public InputReader InputReader;
    Rigidbody rb;
    public GameObject bullet;
    public Transform bulletSpawn;
    public float moveSpeed = 2f;
    [Min(1f)] public float moveAcceleration = 24f;
    [Min(1f)] public float moveBraking = 32f;
    [Min(90f)] public float aimTurnSpeed = 900f;
    [Min(.01f)] public float animationDamping = .12f;
    Quaternion aimRotation;
    public float knockBack = 0.5f;
    public float dashForce = 10f;
    public TakeDamage damage;
    public Animator anim;
    private AudioSource audioSource;
    public AudioSource footstepAudioSource;
    public AudioClip shot;
    public ParticleSystem part;
    CapsuleCollider cc;
    public bool isDead = false;
    public GameManager gm;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        InputReader = GameObject.Find("GameManager").GetComponent<InputReader>();
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 0f;
        aimRotation = rb.rotation;
        SubscribeInput();
        audioSource = GetComponent<AudioSource>();
        cc = GetComponent<CapsuleCollider>();

        gm = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!gm.isPlaying)
        {
            StopFootsteps();
            anim.SetFloat("hor", 0);
            anim.SetFloat("vert", 0);
            if (rb != null) rb.linearVelocity = Vector3.zero;
            return;
        }
        anim.SetBool("isPlaying", true);
        if (isDead)
        {
            StopFootsteps();
            return;
        }
        if (damage.health < 1)
        {
            StopFootsteps();
            isDead = true;
            anim.Play("Death");
            cc.enabled = false;
            Destroy(rb);
            gm.GameOver();
            return;
        }
        RotateToMouse();
        Move();
    }

    void Move()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0;
        Vector3 moveDir = Vector3.ClampMagnitude(velocity / Mathf.Max(.1f, moveSpeed), 1f);
        Vector3 localMove = transform.InverseTransformDirection(moveDir);

        anim.SetFloat("hor", localMove.x, animationDamping, Time.deltaTime);
        anim.SetFloat("vert", localMove.z, animationDamping, Time.deltaTime);

        if (velocity.sqrMagnitude > .08f && Time.time >= dashUntil)
        {
            if (footstepAudioSource && !footstepAudioSource.isPlaying) footstepAudioSource.Play();
        }
        else
        {
            StopFootsteps();
        }
    }

    void FixedUpdate()
    {
        if (rb == null || gm == null || !gm.isPlaying || isDead || damage.health < 1) return;
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, aimRotation, aimTurnSpeed * Time.fixedDeltaTime));
        if (Time.time < dashUntil) return;
        Vector2 input = Vector2.ClampMagnitude(InputReader.Move, 1f);
        Vector3 target = new Vector3(input.x * moveSpeed, rb.linearVelocity.y, input.y * moveSpeed);
        float acceleration = input.sqrMagnitude > .001f ? moveAcceleration : moveBraking;
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, target, acceleration * Time.fixedDeltaTime);
    }

    void StopFootsteps()
    {
        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            footstepAudioSource.Stop();
    }

    bool inputSubscribed;

    void SubscribeInput()
    {
        if (InputReader == null || inputSubscribed) return;
        InputReader.AttackPressed += Shoot;
        InputReader.JumpPressed += Dash;
        inputSubscribed = true;
    }

    void OnEnable()
    {
        SubscribeInput();
    }

    void OnDisable()
    {
        StopFootsteps();
        if (InputReader == null || !inputSubscribed) return;
        InputReader.AttackPressed -= Shoot;
        InputReader.JumpPressed -= Dash;
        inputSubscribed = false;
    }

    void Shoot()
    {
        if (!gm.isPlaying) return;
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Shooting")
            || (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsName("Shooting")) || damage.health < 1)
        {
            return;
        }
        Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation);
        ShotFired?.Invoke(transform.position);
        rb.AddForce(-transform.forward * knockBack, ForceMode.Impulse);
        anim.CrossFadeInFixedTime("Shooting", .06f);
        audioSource.PlayOneShot(shot);
        part.Play();
    }

    float nextDash;
    float dashUntil;

    void Dash()
    {
        if (gm == null || !gm.isPlaying || isDead || damage.health < 1 || rb == null || Time.time < nextDash) return;
        nextDash = Time.time + 1.2f;
        dashUntil = Time.time + .18f;
        rb.AddForce(transform.forward * dashForce, ForceMode.Impulse);
    }

    void RotateToMouse()
    {
        if (Camera.main == null) return;
        // A stable horizontal aim plane avoids snapping when the pointer crosses tall props or actors.
        Plane plane = new Plane(Vector3.up, rb.position);
        Ray ray = Camera.main.ScreenPointToRay(InputReader.Look);
        if (!plane.Raycast(ray, out float distance)) return;
        Vector3 direction = ray.GetPoint(distance) - rb.position;
        direction.y = 0;
        if (direction.sqrMagnitude > .04f) aimRotation = Quaternion.LookRotation(direction);
    }
}
