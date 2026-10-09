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
        Vector2 move = InputReader.Move;
        Vector3 moveDir = new Vector3(move.x, 0, move.y);
        moveDir = Vector3.ClampMagnitude(moveDir, 1f);
        Vector3 localMove = transform.InverseTransformDirection(moveDir);

        anim.SetFloat("hor", localMove.x);
        anim.SetFloat("vert", localMove.z);

        if (move.sqrMagnitude > 0.01f)
        {
            if (!footstepAudioSource.isPlaying) footstepAudioSource.Play();
        }
        else
        {
            StopFootsteps();
        }
    }

    void FixedUpdate()
    {
        if (rb == null || gm == null || !gm.isPlaying || isDead || Time.time < dashUntil) return;
        Vector2 input = Vector2.ClampMagnitude(InputReader.Move, 1f);
        Vector3 target = new Vector3(input.x * moveSpeed, rb.linearVelocity.y, input.y * moveSpeed);
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, target, 35f * Time.fixedDeltaTime);
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
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Shooting") || damage.health < 1)
        {
            return;
        }
        Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation);
        ShotFired?.Invoke(transform.position);
        rb.AddForce(-transform.forward * knockBack, ForceMode.Impulse);
        anim.Play("Shooting");
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
        RaycastHit hit;
        if (Camera.main == null || !Physics.Raycast(Camera.main.ScreenPointToRay(InputReader.Look), out hit, 1000)) return;
        Vector3 direction = (hit.point - transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f) transform.forward = direction;

    }


}
