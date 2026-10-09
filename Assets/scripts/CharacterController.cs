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
        InputReader.AttackPressed += Shoot;
        InputReader.JumpPressed += Dash;
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
        transform.Translate(moveDir * Time.deltaTime * moveSpeed, Space.World);
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

    void StopFootsteps()
    {
        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            footstepAudioSource.Stop();
    }

    void OnDisable()
    {
        StopFootsteps();
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

    void Dash()
    {
        rb.AddForce(transform.forward * dashForce, ForceMode.Impulse);
    }

    void RotateToMouse()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(InputReader.Look), out hit, 1000))
        {
        }
        Vector3 direction = (hit.point - transform.position).normalized;
        direction.y = 0f;

        transform.forward = direction;

    }


}
