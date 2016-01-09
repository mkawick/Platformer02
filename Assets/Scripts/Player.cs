using UnityEngine;
using System.Collections;

// I do not need variable jump height, but here is the video
// https://www.youtube.com/watch?v=rVfR14UNNDo&index=10&list=PLFt_AvWsXl0f0hqURlhyIoAabKPgRsqjz

[RequireComponent(typeof( Controller2D ))]
public class Player : MonoBehaviour {

	// these vectors are used for wall jumps
	public Vector2 wallJumpClimb;
	public Vector2 wallJumpOff;
	public Vector2 wallLeap;// between walls

	public float 	jumpHeight = 4;
	public float 	timeToJumpApex = 0.4f;
	// these values are calculated from the settings above
	float 			gravity;
	float 			jumpVelocity;

	float 			accellerationTimeAirborne = 0.2f;
	float 			accellerationTimeGrounded = 0.1f;
	float 			moveSpeed = 6f;

	public float 	wallSlideSpeedMax = 3;
	public float	wallStickTime = 0.25f;
	public float	wallTimeBeforeUnStick;

	Vector3			velocity;
	Controller2D 	controller;

	float velocityXSmoothing;

	//--------------------------------------------------------------------
	// Use this for initialization
	void Start () {
		controller = GetComponent <Controller2D>();

		gravity = - (2* jumpHeight) / (timeToJumpApex * timeToJumpApex );
		jumpVelocity = Mathf.Abs (gravity) * timeToJumpApex;
		print ( "Gravity = " + gravity + "  jump velocity = " + jumpVelocity);
	}



	// Update is called once per frame
	void Update () {

		Vector2 input = new Vector2( Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw("Vertical") );
		int wallDirX = -1; // controller.collisions.left
		if( controller.collisions.right ) {
			wallDirX = 1;
		}

		//float targetVelocityX = input.x * moveSpeed;
		//velocity.x = Mathf.SmoothDamp( velocity.x, targetVelocityX, ref velocityXSmoothing, smoothTime);
		DampenXDirection( input.x, ref velocity );

		bool wallSliding = DetectWallSliding( (int) input.x, ref velocity, wallDirX );

		if( controller.collisions.above || controller.collisions.below ){
			velocity.y = 0;
		}
		/*if( controller.collisions.left || controller.collisions.right ){
			velocity.x = 0;
		}*/


		if( Input.GetKeyDown( KeyCode.Space ) == true ){ // && controller.collisions.below == true
			if( wallSliding ){
				HandleInputWhileWallSliding( (int) input.x, ref velocity, wallDirX );
			}
			if( controller.collisions.below ) {
				velocity.y = jumpVelocity;
			}
		}

		velocity.y += gravity * Time.deltaTime;
		controller.Move( velocity * Time.deltaTime );
	}

	
	void HandleInputWhileWallSliding ( int inputX, ref Vector3 velocity, int wallDirX ) {
		// climbing jump... hopping up a wall.
		// we are moving in the direction of the wall
		if( wallDirX == inputX ) {
			velocity.x = -wallDirX * wallJumpClimb.x; // jump away from the wall
			velocity.y = wallJumpClimb.y;
		}
		else if( inputX == 0 ) { // falling off the wall
			velocity.x = - wallDirX * wallJumpOff.x;
			velocity.y = wallJumpOff.y;
		}
		else { // jumping betwwen walls .. wallDirX == -inputX
			velocity.x = - wallDirX * wallLeap.x;
			velocity.y = wallLeap.y;
		}
	}

	bool DetectWallSliding ( int inputX, ref Vector3 velocity, int wallDirX ) {
		if( (controller.collisions.left || controller.collisions.right) && controller.collisions.below == false ){
			//print ( "sliding" );
			if( velocity.y < -wallSlideSpeedMax ) {
				velocity.y = -wallSlideSpeedMax;
			}
			
			if( wallTimeBeforeUnStick > 0 ) {
				
				velocityXSmoothing = 0;
				velocity.x = 0;
				
				if( inputX != wallDirX && inputX != 0 ) {// opposite direction
					wallTimeBeforeUnStick -= Time.deltaTime; // don't unstick immediately
				}
				else {
					wallTimeBeforeUnStick = wallStickTime;
				}
			}
			else {// set the value or it never runs
				wallTimeBeforeUnStick = wallStickTime;
			}
			return true; 
		}
		return false;
	}
	
	void DampenXDirection( float inputX, ref Vector3 velocity ) {
		float targetVelocityX = inputX * moveSpeed;
		float smoothTime = (controller.collisions.below == true) ? accellerationTimeGrounded : accellerationTimeAirborne;
		
		velocity.x = Mathf.SmoothDamp( velocity.x, targetVelocityX, ref velocityXSmoothing, smoothTime);
	}
}
