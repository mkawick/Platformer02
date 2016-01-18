using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlatformController : RaycastController {

	//---------------------------------------------------------------
	public LayerMask passengerMask;
	//public Vector3 move;

	public Vector3[] localWaypoints;
	Vector3[] globalWaypoints;

	public float platformSpeed;
	public float percentageMovedBetweenWaypoints;
	int fromWaypointIndex;
	public bool cyclicWaypoint;
	public float waitTimeOnWaypoint;
	[Range(0, 2 )]
	public float easingExponentWaypoint = 1;
	float nextMoveTime;

	List< PassengerMovement > passengerMovementList;
	// optimizing the GetComponent calls.
	Dictionary<Transform, Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D>(); 

	//---------------------------------------------------------------
	// Use this for initialization
	public override void Start () {
		base.Start ();

		globalWaypoints = new Vector3[ localWaypoints.Length ]; 
		for( int i=0; i<localWaypoints.Length; i++ ) {
			globalWaypoints[i] = localWaypoints[i] + transform.position;
		}
	}
	
	// Update is called once per frame
	void Update () {

		UpdateRaycastOrigins();

		Vector3 velocity = CalculatePlatformMovement();

		CalculatePassengerMovement ( velocity );

		MovePassengers ( true );
		transform.Translate( velocity );
		MovePassengers ( false );
	}

	float CalculateEasingPercentage( float x ) {
		float a = easingExponentWaypoint + 1;
		float xToTheA = Mathf.Pow( x, a );
		return xToTheA / ( xToTheA + Mathf.Pow( x-1, a ) );
	}

	Vector3 CalculatePlatformMovement() {

		if( Time.time < nextMoveTime ) {
			return Vector3.zero;
		}

		if( globalWaypoints.Length < 1 )
			return Vector3.zero;

		//return move * Time.deltaTime;
		int toWaypointIndex = fromWaypointIndex + 1;
		if( toWaypointIndex >= globalWaypoints.Length ) {
			toWaypointIndex = 0;
		}
		float distanceBetweenWaypoints = Vector3.Distance( globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex] );
		percentageMovedBetweenWaypoints += Time.deltaTime * platformSpeed / distanceBetweenWaypoints;
		Mathf.Clamp01( percentageMovedBetweenWaypoints );

		Vector3 newPosition = Vector3.Lerp( globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex], CalculateEasingPercentage( percentageMovedBetweenWaypoints ) );

		if( percentageMovedBetweenWaypoints >= 1 ) {
			percentageMovedBetweenWaypoints = 0;
			fromWaypointIndex ++;
			if( cyclicWaypoint == false ) {
				if( fromWaypointIndex >= globalWaypoints.Length-1 ) {
					fromWaypointIndex = 0;
					System.Array.Reverse( globalWaypoints );// simple way to retrace our steps
				}
			}
			else {
				if( fromWaypointIndex >= globalWaypoints.Length ) {
					fromWaypointIndex = 0;
				}
			}

			nextMoveTime = Time.time + waitTimeOnWaypoint;
		}
		return newPosition - transform.position;
	}

	void MovePassengers( bool beforeMovePLatform ) {
		foreach( PassengerMovement passenger in passengerMovementList ){

			if( passengerDictionary.ContainsKey( passenger.transform ) == false ){
				passengerDictionary.Add( passenger.transform, passenger.transform.GetComponent<Controller2D> () );
			}

			if( passenger.moveBeforePlatform == beforeMovePLatform ) {
				passengerDictionary[passenger.transform].Move( passenger.velocity, passenger.standingOnPLatform );
			}
		}
	}

	void CalculatePassengerMovement( Vector3 velocity ) {
		HashSet<Transform> movedPassengers = new HashSet<Transform>();
		passengerMovementList = new List<PassengerMovement>();

		float directionX = Mathf.Sign ( velocity.x );
		float directionY = Mathf.Sign ( velocity.y );
		bool isMovingUp = directionY == 1;

		// grab anyone near the vertically platform
		if( velocity.y != 0 ) {
			float rayLength = Mathf.Abs( velocity.y ) + SkinWidth;
			
			for( int i=0; i< verticalRayCount; i++ ){
				Vector2 rayOrigin = ( directionY == -1)? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
				rayOrigin += Vector2.right * ( verticalRaySpacing * i );
				RaycastHit2D hit = Physics2D.Raycast( rayOrigin, Vector2.up * directionY, rayLength, passengerMask );

				if( hit ) {

					// move each passeneger only once
					if( movedPassengers.Contains ( hit.transform ) == false ) {
						movedPassengers.Add ( hit.transform );
						float pushX = ( directionY == 1 )? velocity.x: 0;
						float pushY = velocity.y - (hit.distance - SkinWidth ) * directionY;

						//hit.transform.Translate ( new Vector3( pushX, pushY) );
						passengerMovementList.Add( new PassengerMovement( hit.transform, new Vector3( pushX, pushY), isMovingUp, true ) );
					}
				}
			}
		}

		// horizontal
		if( velocity.x != 0 ) {
			//movedPassengers.Clear();
			float rayLength = Mathf.Abs( velocity.x ) + SkinWidth;
			
			for( int i=0; i< horizontalRayCount; i++ ){
				Vector2 rayOrigin = ( directionX == -1)? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
				rayOrigin += Vector2.up * ( horizontalRaySpacing * i );
				RaycastHit2D hit = Physics2D.Raycast( rayOrigin, Vector2.right * directionX, rayLength, passengerMask );
				
				if( hit ) {
					
					// move each passeneger only once
					if( movedPassengers.Contains ( hit.transform ) == false ) {
						movedPassengers.Add ( hit.transform );
						float pushX = velocity.x - (hit.distance - SkinWidth ) * directionX;
						// small downward force to keep us on the ground to allow jumping when pushed from the side
						float pushY = -SkinWidth; 
						
						//hit.transform.Translate ( new Vector3( pushX, pushY) );
						// we're being pushed by the platform from the side so we are not on the platform
						passengerMovementList.Add( new PassengerMovement( hit.transform, new Vector3( pushX, pushY), false, true ) );
					}
				}
			}
		}

		// passengers standing on the platform
		if( directionY == -1 || velocity.y == 0 && velocity.x != 0 ) { // upward.
			//movedPassengers.Clear();
			float rayLength = SkinWidth * 2;
			
			for( int i=0; i< verticalRayCount; i++ ){
				Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * ( verticalRaySpacing * i );
				RaycastHit2D hit = Physics2D.Raycast( rayOrigin, Vector2.up, rayLength, passengerMask );
				
				if( hit ) {
					
					// move each passeneger only once
					if( movedPassengers.Contains ( hit.transform ) == false ) {
						movedPassengers.Add ( hit.transform );
						float pushX = velocity.x;
						float pushY = velocity.y;
						
						//hit.transform.Translate ( new Vector3( pushX, pushY) );
						passengerMovementList.Add( new PassengerMovement( hit.transform, new Vector3( pushX, pushY), true, false ) );

					}
				}
			}
		}
	}

	struct PassengerMovement {
		public Transform transform;
		public Vector3 velocity;
		public bool standingOnPLatform;
		public bool moveBeforePlatform;

		public PassengerMovement( Transform _transform, Vector3 _velocity, bool _standingOnPLatform, bool _moveBeforePlatform ) {
			transform = _transform;
			velocity = _velocity;
			standingOnPLatform = _standingOnPLatform;
			moveBeforePlatform = _moveBeforePlatform;
		}
	}

	void OnDrawGizmos() {
		if( localWaypoints != null ) {
			Gizmos.color = Color.red;
			float lineLength = 0.3f;

			int num = localWaypoints.Length;
			for( int i=0; i<num; i++ ){
				Vector3 globalWaypointPosition = localWaypoints[i] + transform.position;
				if( Application.isPlaying == true ) {
					globalWaypointPosition = globalWaypoints[i];
				}

				// draw cross
				Vector3 offset = Vector3.up * lineLength;// vertical
				Gizmos.DrawLine( globalWaypointPosition - offset, globalWaypointPosition + offset );

				offset = Vector3.left * lineLength; // horizontal
				Gizmos.DrawLine( globalWaypointPosition - offset, globalWaypointPosition + offset );
			}
		}
	}
}
