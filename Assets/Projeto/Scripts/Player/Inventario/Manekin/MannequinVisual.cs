using UnityEngine;

public class MannequinVisual : MonoBehaviour
{
    public GameObject rightArmModel;
    public GameObject leftLegModel;
    public GameObject keyVisual;

    private void Reset()
    {
        if (rightArmModel != null) rightArmModel.SetActive(false);
        if (leftLegModel != null) leftLegModel.SetActive(false);
        if (keyVisual != null) keyVisual.SetActive(false);
    }

    public void ShowPart(PartType part)
    {
        if (part == PartType.RightArm && rightArmModel != null) rightArmModel.SetActive(true);
        if (part == PartType.LeftLeg && leftLegModel != null) leftLegModel.SetActive(true);
    }

    public void SetKeyVisible(bool visible)
    {
        if (keyVisual != null) keyVisual.SetActive(visible);
    }
}
