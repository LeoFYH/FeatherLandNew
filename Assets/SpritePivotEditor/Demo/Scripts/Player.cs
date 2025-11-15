using UnityEngine;
using System.Collections;


namespace SpritePivotEditor{

	public class Player : MonoBehaviour {


		public Animator	animator;
		public float	speed;
		private int		special = Animator.StringToHash("Base Layer.Special");
		private bool	isFacingRight = true;
		//-----------------------


		//-----------------------
		void Awake(){


			animator = GetComponent<Animator>();
		}
		//-----------------------
		
		
		//-----------------------
		void Update () {
		
			AnimatorStateInfo currentStateInfo = animator.GetCurrentAnimatorStateInfo(0);

			float input = Input.GetAxis ("Horizontal");

			animator.SetFloat("Input", Mathf.Abs(input));


			if(currentStateInfo.fullPathHash != special){

				//do special
				if(Input.GetKeyDown(KeyCode.Space)){

					animator.SetTrigger("Special");
				}

				//flip sprite 
				if(input < 0 && isFacingRight){

					Flip();
				}
				else if(input > 0 && !isFacingRight){

					Flip();
				}

				//move
				transform.Translate(input * speed * Vector2.right * Time.deltaTime);

			}
		}
		//-----------------------
		
		
		//-----------------------
		private void Flip(){

			isFacingRight = !isFacingRight;

			Vector3 localScale = transform.localScale;
			localScale.x *= -1;
			transform.localScale = localScale;
		}
	}
}
