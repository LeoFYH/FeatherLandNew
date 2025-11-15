using UnityEngine;
using System.Collections;


namespace SpritePivotEditor{

	public class CameraFollow : MonoBehaviour {


		public Transform	target;


		void Update () {
		
			//move with the target along the x axis
			transform.position = new Vector3(target.position.x, transform.position.y, transform.position.z);
		}
	}
}
