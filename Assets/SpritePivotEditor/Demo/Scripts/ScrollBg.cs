using UnityEngine;
using System.Collections;

namespace SpritePivotEditor{

	public class ScrollBg : MonoBehaviour {


		public bool leftSide;
		private const float	size = 28.8f;

		void OnTriggerEnter2D(Collider2D other){

			//Bg is the only gameobject with an trigger so this will work for the Bg only
			if(leftSide){

				//move to right side
				Vector3 pos = other.transform.localPosition;
				pos.x += size;
				other.transform.localPosition = pos;
			}
			else{

				//move to left side
				Vector3 pos = other.transform.localPosition;
				pos.x += -size;
				other.transform.localPosition = pos;
			}

		
		}
	}
}
