using UnityEngine;
using System.Collections;

public class ReplayData  {

    private float[] oldState;
    private int action;
    private float reward;
    private float[] newState;

    public ReplayData(float[] oldState, int action, float reward, float[] newState)
    {
        this.oldState = new float[oldState.Length];
        this.newState = new float[newState.Length];

        for (int i = 0; i < oldState.Length; i++)
            this.oldState[i] = oldState[i];

        this.action = action;
        this.reward = reward;

        for (int i = 0; i < newState.Length; i++)
            this.newState[i] = newState[i];
    }
}
