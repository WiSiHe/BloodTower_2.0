using UnityEngine;

public class RiderJointListener : MonoBehaviour
{
    // If the joint on this object breaks, we’ve been thrown off.
    void OnJointBreak2D(Joint2D brokenJoint)
    {
        // You can add knockoff VFX/SFX here or trigger a brief stun.
        // Nothing else required: the goat script also cleans itself up on next buck/attempt or mount input.
    }
}
