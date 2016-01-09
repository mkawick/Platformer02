using UnityEngine;
using System.Collections;


public class Controller2D : RaycastController {
	
	float 					maxClimbAngle = 80;
	float 					maxDescendAngle = 80;
	
	public CollisionInfo 	collisions;
	[HideInInspector]
	public Vector2 			playerInput;

	public override void Start() {
		base.Start();
		collisions.facingDirection = 1;
	}
	// Update is called once per frame
	/*void Update () {

	}*/

	public void Move( Vector3 velocity, bool standingOnPlatform = false ) {
		UpdateRaycastOrigins();
		collisions.Reset ();

		collisions.velocityOld = velocity;

		if( velocity.x != 0 ) {
			collisions.facingDirection = (int) Mathf.Sign( velocity.x );
		}
		if( velocity.y < 0 ) {
			DescendSlope( ref velocity );
		}

		// in order to handle walls, we always check for horizontal collisions
		HorizontalCollisions( ref velocity );
		if( velocity.y != 0 ) {
			VerticalCollisions( ref velocity );
		}

		transform.Translate( velocity );

		if( standingOnPlatform == true ) {
			collisions.below = true;
		}
	}

	void HorizontalCollisions( ref Vector3 velocity ) {
		
		float directionX = collisions.facingDirection;
		float rayLength = Mathf.Abs( velocity.x ) + SkinWidth;

		if( Mathf.Abs ( velocity.x ) < SkinWidth ) {
			rayLength = 2*SkinWidth;
		}
		
		for( int i=0; i< horizontalRayCount; i++ ){
			Vector2 rayOrigin = ( directionX == -1)? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
			rayOrigin += Vector2.up * ( horizontalRaySpacing * i );
			
			RaycastHit2D hit = Physics2D.Raycast( rayOrigin, Vector2.right * directionX, rayLength, collisionMask );
			
			Debug.DrawRay( rayOrigin, Vector2.right * directionX * rayLength, Color.red );

			if( hit ){

				// we are inside another object
				if( hit.distance == 0 ) {
					continue;
				}
				// get the angle of the surface
				float slopeAngle = Vector2.Angle( hit.normal, Vector2.up );

				if( i == 0 && slopeAngle <= maxClimbAngle ) {
					if( collisions.descendingSlope ) {
						collisions.descendingSlope = false;
						velocity = collisions.velocityOld;
					}
					//print( slopeAngle );
					float distanceToSlopeStart = 0;
					if( slopeAngle != collisions.slopeAngleOld ){
						distanceToSlopeStart = hit.distance - SkinWidth;
						velocity.x -= distanceToSlopeStart * directionX;// clamp to surface
					}
					ClimbSlope(ref velocity, slopeAngle );
					// add the amount that we removed back onto the value
					// 0 will save a condition check
					velocity.x += distanceToSlopeStart * directionX;
				}

				if( collisions.climbingSlope == false || slopeAngle > maxClimbAngle) {
					velocity.x = ( hit.distance - SkinWidth )* directionX;
					rayLength = hit.distance;

					if( collisions.climbingSlope ) {
						velocity.y = Mathf.Tan ( collisions.slopeAngle * Mathf.Deg2Rad ) * Mathf.Abs ( velocity.x );
					}
					collisions.left = (directionX == -1);
					collisions.right = (directionX == 1);
				}
			}
		}
	}

	void VerticalCollisions( ref Vector3 velocity ) {

		float directionY = Mathf.Sign( velocity.y );
		float rayLength = Mathf.Abs( velocity.y ) + SkinWidth;

		for( int i=0; i< verticalRayCount; i++ ){
			Vector2 rayOrigin = ( directionY == -1)? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
			rayOrigin += Vector2.right * ( verticalRaySpacing * i  + velocity.x );

			RaycastHit2D hit = Physics2D.Raycast( rayOrigin, Vector2.up * directionY, rayLength, collisionMask );

			Debug.DrawRay( rayOrigin, Vector2.up * directionY * rayLength, Color.red );

			if( hit ){
				velocity.y = ( hit.distance - SkinWidth )* directionY;
				rayLength = hit.distance;

				if( collisions.climbingSlope ) {
					velocity.x = velocity.y / Mathf.Tan( collisions.slopeAngle * Mathf.Deg2Rad ) * Mathf.Sign ( velocity.x );
				}

				collisions.below = (directionY == -1);
				collisions.above = (directionY == 1);
			}
		}

		if( collisions.climbingSlope ) {
			// look for a new slope... dealing with intersecting slopes
			float directionX = Mathf.Sign ( velocity.x );
			rayLength = Mathf.Abs( velocity.x ) + SkinWidth;
			Vector2 rayOrigin ;
			if( directionX == -1 ) 
				rayOrigin = raycastOrigins.bottomLeft;
			else
				rayOrigin = raycastOrigins.bottomRight;
			rayOrigin += Vector2.up * velocity.y;// cast from new height

			RaycastHit2D hit = Physics2D.Raycast ( rayOrigin, Vector2.right * directionX, rayLength, collisionMask );

			if( hit ) {
				float slopeAngle = Vector2.Angle( hit.normal, Vector2.up );
				if( slopeAngle != collisions.slopeAngle ) {
					velocity.x = (hit.distance - SkinWidth ) * directionX;
					collisions.slopeAngle = slopeAngle;
				}
			}
		}
	}

	// basic idea is to keep our speed even up an incline
	void ClimbSlope( ref Vector3 velocity, float slopeAngle ) {

		float moveDistance = Mathf.Abs( velocity.x );
		float climbVelocityY = moveDistance * Mathf.Sin( slopeAngle * Mathf.Deg2Rad );

		if( velocity.y <= climbVelocityY ) {// we are jumping
			velocity.y = climbVelocityY;
			velocity.x = moveDistance * Mathf.Cos( slopeAngle * Mathf.Deg2Rad ) * Mathf.Sign( velocity.x );

			// assume that we are on the ground
			collisions.below = true;
			collisions.climbingSlope = true;
			collisions.slopeAngle = slopeAngle;
		}
	}

	void DescendSlope( ref Vector3 velocity ) { // maxClimbAngle
		float directionX = Mathf.Sign ( velocity.x );
		//rayLength = Mathf.Abs( velocity.x ) + SkinWidth;
		Vector2 rayOrigin ;
		if( directionX == -1 ) 
			rayOrigin = raycastOrigins.bottomRight; // opposite direction
		else
			rayOrigin = raycastOrigins.bottomLeft;
		//rayOrigin += Vector2.up * velocity.y;// cast from new height
		RaycastHit2D hit = Physics2D.Raycast( rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask );

		if( hit ){
			float slopeAngle = Vector2.Angle( hit.normal, Vector2.up );
			if( slopeAngle != 0 && slopeAngle <= maxDescendAngle ) {
				if( Mathf.Sign( hit.normal.x ) ==  directionX ) {
					// close to slope
					if( hit.distance - SkinWidth <= Mathf.Tan (slopeAngle * Mathf.Deg2Rad) * Mathf.Abs( velocity.x ) ) {
						float moveDistance = Mathf.Abs( velocity.x );
						float descendVelocityY = moveDistance * Mathf.Sin( slopeAngle * Mathf.Deg2Rad );
						velocity.x = moveDistance * Mathf.Cos( slopeAngle * Mathf.Deg2Rad ) * Mathf.Sign( velocity.x );
						velocity.y -= descendVelocityY;

						// assume that we are on the ground
						collisions.descendingSlope = true;
						collisions.below = true;
						collisions.slopeAngle = slopeAngle;
					}
				}
			}
		}
	}


	public struct CollisionInfo {
		public bool above, below;
		public bool left, right;

		public bool climbingSlope;
		public bool descendingSlope;
		public float slopeAngle;
		public float slopeAngleOld;
		public Vector3 velocityOld;
		public int facingDirection;

		public void Reset() {
			above = below = false;
			left = right = false;
			climbingSlope = false;
			descendingSlope = false;
			slopeAngleOld = slopeAngle;
			slopeAngle = 0;
		}
	}

}
