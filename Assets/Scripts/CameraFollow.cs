using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour {

	public Controller2D target;
	public float 		verticalOffset;
	public float 		lookAheadDistanceX;
	public float		lookSmoothTime;
	public float		verticalSmoothTime;
	public Vector2 		focusAreaSize;

	FocusArea 			focusArea;

	float currentLookAheadX;
	float targetLookAheadX;
	float lookAheadDirectionX;
	float smoothLookVelocityX;
	float smoothVelocityY;

	bool	isLookAheadStopped;

	//----------------------------------------------
	// Use this for initialization
	void Start () {
		focusArea = new FocusArea( target.collider.bounds, focusAreaSize );
	}

	void	LateUpdate() {// after all player movement is done, this is invoked
		focusArea.Update( target.collider.bounds );

		Vector2 focusPosition = CalculateDamping();

		transform.position = (Vector3) (focusPosition) + Vector3.forward * -10;
	}

	Vector2	CalculateDamping() {
		Vector2 focusPosition = focusArea.center + Vector2.up * verticalOffset;

		bool shouldDoLookAhead = true;
		if( shouldDoLookAhead == true ) {
			if( focusArea.velocity.x != 0 ) {
				lookAheadDirectionX = Mathf.Sign ( lookAheadDirectionX );
				if( Mathf.Sign( target.playerInput.x ) == Mathf.Sign( focusArea.velocity.x ) && 
				   target.playerInput.x != 0) {
					isLookAheadStopped = false;
					targetLookAheadX = lookAheadDirectionX * lookAheadDistanceX;
				}
			}
			else {
				if( isLookAheadStopped == false ) {
					isLookAheadStopped = true;
					targetLookAheadX = currentLookAheadX + (lookAheadDirectionX * lookAheadDistanceX - currentLookAheadX ) / 3f;
					///lookAheadDirectionX = 0;
				}
			}
			
		}
		// damping 
		currentLookAheadX = Mathf.SmoothDamp( currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, lookSmoothTime );
		
		focusPosition.y = Mathf.SmoothDamp( transform.position.y, focusPosition.y, ref smoothVelocityY, verticalSmoothTime );
		focusPosition += Vector2.right*currentLookAheadX;
		return focusPosition;
	}
	void	OnDrawGizmos() {
		focusArea.DrawArea( focusAreaSize );
	}
	/*
	// Update is called once per frame
	void Update () {
	
	}*/

	struct FocusArea {
		public Vector2 center;
		public Vector2 velocity;
		float left, right;
		float top, bottom;

		public FocusArea( Bounds targetBounds, Vector2 size ) {
			float halfWidth = size.x/2;
			float halfHeight = size.y/2;

			left = targetBounds.center.x - halfWidth;
			right = targetBounds.center.x + halfWidth;
			top = targetBounds.min.y + halfHeight;
			bottom = targetBounds.min.y;// note, not centered

			velocity = Vector2.zero;

			center = Vector2.zero;

			UpdateCenter();
		}

		//--------------------------------------------

		public void Update( Bounds targetBounds ) {
			float moveX = 0;
			if( targetBounds.min.x < left ) {
				moveX = targetBounds.min.x - left;
			}
			else if( targetBounds.max.x > right ) {
				moveX = targetBounds.max.x - right;
			}

			left += moveX;
			right += moveX;
			//////////////////////////////
			float moveY = 0;
			if( targetBounds.min.y < bottom ) {
				moveY = targetBounds.min.y - bottom;
			}
			else if( targetBounds.max.y > top ) {
				moveY = targetBounds.max.y - top;
			}
			
			top += moveY;
			bottom += moveY;

			UpdateCenter();
			velocity = new Vector2( moveX, moveY );
		}

		//--------------------------------------------

		void	UpdateCenter() {
			center = new Vector2( (left+right)/2, (top+bottom)/2 );// more flexible in future
		}

		public void	DrawArea( Vector2 focusAreaSize ) {
			Gizmos.color = new Color( 1, 0, 0, 0.5f );
			Gizmos.DrawCube( center, focusAreaSize );
		}
	}
}
