using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents; 
using Unity.MLAgents.Sensors; 
using Unity.MLAgents.Actuators;

public class Agent1 : Agent
{
    public EpisodeManager episodeManager;
    Rigidbody rBody;
    private float timer = 0f;           // Timer variable to track elapsed time
    [SerializeField] float maxTimePerEpisode = 20f;  // Maximum time allowed per episode (in seconds)
    [SerializeField] System.String TargetTag = "Hider"; // The target object
    private Transform Target; // The target object
    private Transform AgentArea;
    [SerializeField] float forceMultiplier = 3;
    public float InputX;
    public float InputZ;
    public Animator anim;  // Reference to the Animator
    public MovementInput movementInput;  // Reference to the MovementInput script
      // Define animation smoothing times similar to the MovementInput script
    private Vector3 lastPosition;
    [Header("Animation Smoothing")]
    [Range(0, 1f)]
    public float HorizontalAnimSmoothTime = 0.2f;
    [Range(0, 1f)]
    public float VerticalAnimTime = 0.2f;

    void Start () {
        rBody = GetComponent<Rigidbody>(); // Get the Rigidbody component in ordee to apply forces, reset velocity...
        anim = GetComponent<Animator>();
        movementInput = GetComponent<MovementInput>();  // Get the MovementInput component
        AgentArea = transform.parent;
        Target = AgentArea.Find(TargetTag);
        if (Target == null)
        {
            Debug.LogError("Target Catcher not found! Make sure the target is assigned and tagged correctly.");
        }
    }

    
    public override void OnEpisodeBegin()
    {
       // Move the target to a new spot
        // local position with respect to the parent object
        episodeManager.resetEpisode();
        this.transform.localPosition = new Vector3(Random.value * 12 - 4,0.5f,Random.value * 12 - 4);

        rBody.velocity = Vector3.zero;  // Reset the velocity
        timer = 0f;
    }
    public override void CollectObservations(VectorSensor sensor)
    {
    // Target and Agent positions
    //sensor.AddObservation(Target.localPosition); // target position
    sensor.AddObservation(this.transform.localPosition); // agent position

    // Agent velocity
    //sensor.AddObservation(rBody.velocity.x);
    //sensor.AddObservation(rBody.velocity.z);
    // therefore we have 8 continuous observations
    }
    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];  // continuous action 0 mapping, we have 2 continuous actions in editor behaviour parameters.
        controlSignal.z = actionBuffers.ContinuousActions[1];
        // rBody.AddForce(controlSignal * forceMultiplier); // this actually defines the continuous by applying force to the agent.
        InputX = controlSignal.x;
        InputZ = controlSignal.z;
        // Animate based on network-provided input
                                //anim.SetFloat("InputX", InputX, HorizontalAnimSmoothTime, Time.deltaTime);
                                //anim.SetFloat("InputZ", InputZ, VerticalAnimTime, Time.deltaTime);
        
        // Rewards
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);
        
        movementInput.InputX = InputX;
        movementInput.InputZ = InputZ;
        movementInput.PlayerMoveAndRotation();
        movementInput.InputMagnitude();
        // Reached target
        if (distanceToTarget < 1.42f)
        {
            episodeManager.EndEpisodeForBothAgents(false); // false -> catcher wins
        }
        // Fell off platform
        
        float previousDistance = Vector3.Distance(lastPosition, Target.position);
        float currentDistance = Vector3.Distance(this.transform.localPosition, Target.localPosition);

        if (currentDistance < previousDistance) {
            AddReward(0.1f);  // Give reward for getting closer
        } else {
            AddReward(-0.1f); // Penalize for moving further away
        }
        if (Vector3.Distance(lastPosition, this.transform.localPosition) < 0.01f)
        {
            AddReward(-0.1f); // Penalize for being idle
        }


        lastPosition = this.transform.localPosition;  
        timer += Time.deltaTime;  // Increment the timer
        if (timer > maxTimePerEpisode)
        {
            episodeManager.EndEpisodeForBothAgents(true); // true -> catcher loose 
        }

        
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

}
