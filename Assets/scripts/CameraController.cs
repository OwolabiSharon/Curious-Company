using UnityEngine;
using System.Collections;


public class CameraController : MonoBehaviour
{
    public Transform player;
    public Vector3 offset;
    public Vector3 panOffset;
    public Transform[] locations;
    public GameManager gm;
    public int index = 0;
    public float panSpeed = 5f;
    public float followSpeed = 5f;
    public float rotationSpeed = 60f; // Degrees per second
    public bool isPlaying = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (gm.isPlaying)
        {
            isPlaying = true;
        }
        gm = GameObject.Find("GameManager").GetComponent<GameManager>();

    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!gm.isPlaying) return;
        if (!isPlaying)
        {
            Quaternion targetRotation = Quaternion.Euler(70f, 0f, 0f);
            Vector3 targetPosition = player.position + offset;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                panSpeed * Time.deltaTime
            );

            bool rotationReached =
                Quaternion.Angle(transform.rotation, targetRotation) < 0.1f;

            bool positionReached =
                Vector3.Distance(transform.position, targetPosition) < 0.01f;

            if (positionReached)
            {
                transform.rotation = targetRotation;
                transform.position = targetPosition;
                isPlaying = true;
            }

            return; // Wait before running Pan() or Follow()
        }
        // if (index < locations.Length)
        // {
        //     Pan();
        // }
        // else
        // {
        //     Follow();
        // }
        Follow();

    }

    void Pan()
    {
        Vector3 target = locations[index].position + panOffset;
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            panSpeed * Time.deltaTime

        );
        if (Vector3.Equals(transform.position, target))
        {
            index++;
            Debug.Log("nut");
        }
    }

    void Follow()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            player.position + offset,
            followSpeed * Time.deltaTime

        );


        // transform.position = player.position + offset;
    }
}
