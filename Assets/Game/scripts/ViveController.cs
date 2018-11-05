// Obsolete: With Camera (eye) still selected, add a Steam VR_Update Poses component to it. This fixes a bug introduced  // in Unity 5.6 where the controller wouldn’t be tracked.

using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class ViveController : MonoBehaviour
{
    public Hand hand;

    public float squeeze { get { return SteamVR_Input._default.inActions.Squeeze.GetAxis(hand.handType); } }
    public Vector2 touchPos { get { return SteamVR_Input._default.inActions.TouchPos.GetAxis(hand.handType); } }

    public event System.EventHandler<bool> gripToggled = delegate { };
    public event System.EventHandler<bool> isPressingGrip = delegate { };

    public event System.EventHandler<bool> pinchToggled = delegate { };
    public event System.EventHandler<bool> isPressingPinch = delegate { };

    public event System.EventHandler<bool> interactUIToggled = delegate { };
    public event System.EventHandler<bool> isInteractingWithUI = delegate { };

    public event System.EventHandler<bool> trackpadTouched = delegate { };
    public event System.EventHandler<bool> IsTouchingTrackpad = delegate { };

    private void Start()
    {
        if (!hand)
        {
            hand = gameObject.GetComponent<Hand>();
        }
    }

    private void Update()
    {
        if (!hand)
            return;

        isPressingGrip(this, SteamVR_Input._default.inActions.GrabGrip.GetState(hand.handType));
        if (SteamVR_Input._default.inActions.GrabGrip.GetStateDown(hand.handType))
            gripToggled(this, true);
        if (SteamVR_Input._default.inActions.GrabGrip.GetStateUp(hand.handType))
            gripToggled(this, false);

        isPressingPinch(this, SteamVR_Input._default.inActions.GrabPinch.GetState(hand.handType));
        if (SteamVR_Input._default.inActions.GrabPinch.GetStateDown(hand.handType))
            pinchToggled(this, true);
        if (SteamVR_Input._default.inActions.GrabPinch.GetStateUp(hand.handType))
            pinchToggled(this, false);

        isInteractingWithUI(this, SteamVR_Input._default.inActions.InteractUI.GetState(hand.handType));
        if (SteamVR_Input._default.inActions.InteractUI.GetStateDown(hand.handType))
            interactUIToggled(this, true);
        if (SteamVR_Input._default.inActions.InteractUI.GetStateUp(hand.handType))
            interactUIToggled(this, false);

        IsTouchingTrackpad(this, SteamVR_Input._default.inActions.Touchpad.GetState(hand.handType));
        if (SteamVR_Input._default.inActions.Touchpad.GetStateDown(hand.handType))
            trackpadTouched(this, true);
        if (SteamVR_Input._default.inActions.Touchpad.GetStateUp(hand.handType))
            trackpadTouched(this, false);
    }
}