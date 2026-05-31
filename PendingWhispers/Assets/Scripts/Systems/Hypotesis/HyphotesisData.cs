using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cases/Hypothesis")]
public class HypothesisData : ScriptableObject
{
    public string hypothesisID;

    public List<string> textParts;

    public List<HypothesisSlotDefinition> slots;

    public List<string> correctAnswers;
}