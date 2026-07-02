# Description
To add a custom hand state, simply create a class that inherits from ICustomHandState, compile it into a DLL, and load it alongside this mod.
The CustomHold.cs file contains a custom hand state example.

# Instructions
Members from ICustomHandState:
## Method
- Initialize: Called in *CustomHand.Start()*.
- OnStateEnter: Called when entering the hand state.
- OnStateExit: Called when exiting the hand state.
- FixedUpdate: Called in *CustomHand.FixedUpdate()*.
## Field
- Id: The id of your hand state. Forbid less than 3.
- Name: The display name of your hand state.
- RGB: The color of the keyframe when the keyframe has a hand state value.
