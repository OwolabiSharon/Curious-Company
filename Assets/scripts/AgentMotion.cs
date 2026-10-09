using UnityEngine;
using UnityEngine.AI;

// Shared navigation presentation. The agent owns position; physics never competes with it.
public sealed class AgentMotion
{
    readonly NavMeshAgent agent;
    readonly Animator animator;
    readonly bool hasLocomotionSpeed;
    readonly NavMeshPath candidatePath = new NavMeshPath();
    Vector3 lastDestination;
    float nextRepath;
    float turnVelocity;
    bool hasDestination;
    bool moving;

    public AgentMotion(NavMeshAgent agent, Animator animator, Rigidbody body, float acceleration)
    {
        this.agent = agent;
        this.animator = animator;
        agent.updateRotation = false;
        agent.acceleration = Mathf.Max(1f, acceleration);
        agent.autoBraking = true;
        if (body)
        {
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
        }
        animator.applyRootMotion = false;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == "LocomotionSpeed" && parameter.type == AnimatorControllerParameterType.Float)
                hasLocomotionSpeed = true;
        }
    }

    public void SetDestination(Vector3 destination, float interval = .18f)
    {
        if (!agent.isOnNavMesh || Time.time < nextRepath) return;
        nextRepath = Time.time + interval;
        bool displaced = !agent.hasPath && !agent.pathPending
            && (agent.transform.position - destination).sqrMagnitude > 4f;
        if (hasDestination && !displaced && (lastDestination - destination).sqrMagnitude < .12f) return;
        if (agent.SetDestination(destination))
        {
            lastDestination = destination;
            hasDestination = true;
        }
    }

    public bool CanReach(Vector3 destination)
    {
        return agent.isOnNavMesh && agent.CalculatePath(destination, candidatePath)
            && candidatePath.status == NavMeshPathStatus.PathComplete;
    }

    public void Stop(bool clearPath = false)
    {
        if (!agent.isOnNavMesh) return;
        agent.isStopped = true;
        if (clearPath)
        {
            if (agent.hasPath || agent.pathPending) agent.ResetPath();
            hasDestination = false;
            nextRepath = 0;
        }
    }

    public void Resume()
    {
        if (agent.isOnNavMesh) agent.isStopped = false;
    }

    public void Tick(float turnSmoothTime, float turnSpeed, float referenceRunSpeed)
    {
        Vector3 velocity = agent.isOnNavMesh && !agent.isStopped ? agent.velocity : Vector3.zero;
        velocity.y = 0;
        float speed = velocity.magnitude;
        // Different start/stop thresholds prevent a crowd's tiny avoidance adjustments from flickering idle/run.
        moving = moving ? speed > .07f : speed > .18f;
        animator.SetBool("isMoving", moving);
        if (hasLocomotionSpeed)
            animator.SetFloat("LocomotionSpeed", Mathf.Clamp(speed / Mathf.Max(.1f, referenceRunSpeed), .45f, 1.4f), .12f, Time.deltaTime);
        if (speed > .12f)
        {
            float target = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(agent.transform.eulerAngles.y, target, ref turnVelocity,
                Mathf.Max(.02f, turnSmoothTime), turnSpeed, Time.deltaTime);
            agent.transform.rotation = Quaternion.Euler(0, angle, 0);
        }
        else turnVelocity = 0;
    }
}
