using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using PA_DronePack;
using Unity.Mathematics;

public class DroneAgent : Agent
{
    private PA_DroneController dcoScript;

    public DroneSetting area;
    public GameObject goal;

    float preDist;

    private Transform agentTrans;
    private Transform goalTrans;

    private Rigidbody agent_Rigidbody;

    public override void Initialize()
    {
        dcoScript = gameObject.GetComponent<PA_DroneController>();
        agentTrans = gameObject.transform;
        goalTrans = goal.transform;
        
        agent_Rigidbody = gameObject.GetComponent<Rigidbody>();

        Academy.Instance.AgentPreStep += WaitTimeInference;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(agentTrans.position - goalTrans.position);

        sensor.AddObservation(agent_Rigidbody.velocity);

        sensor.AddObservation(agent_Rigidbody.angularVelocity);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(-0.01f);

        var action = actions.ContinuousActions;

        float moveX = Mathf.Clamp(action[0], -1, 1f);
        float moveY = Mathf.Clamp(action[1], -1, 1f);
        float moveZ = Mathf.Clamp(action[2], -1, 1f);

        Debug.Log(moveX + " " + moveY + " " + moveZ);

        dcoScript.DriveInput(moveX);
        dcoScript.StrafeInput(moveY);
        dcoScript.LiftInput(moveZ);

        float distance = Vector3.Magnitude(goalTrans.position - agentTrans.position);

        if(distance <= 0.5f)
        {
            SetReward(1f);
            EndEpisode();
        }
        else if (distance > 10f)
        {
            SetReward(-1f);
            EndEpisode();
        }
        else
        {
            float reward = preDist - distance;
            SetReward(reward);
            preDist = distance;
        }
    }

    public override void OnEpisodeBegin()
    {
        area.AreaSetting();

        preDist = Vector3.Magnitude(goalTrans.position - agentTrans.position);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
        continuousActionsOut[2] = Input.GetAxis("Mouse ScrollWheel");
    }

    public float DecisionWaitingTime = 5f;
    float m_currentTime = 0f;

    public void WaitTimeInference(int action)
    {
        if (Academy.Instance.IsCommunicatorOn)
        {
            RequestDecision();
        }
        else
        {
            if(m_currentTime >= DecisionWaitingTime)
            {
                m_currentTime = 0;
                RequestDecision();
            }
            else
            {
                m_currentTime += Time.fixedDeltaTime;
            }
        }
    }
    
}
