using UnityEngine;
using UnityEditor;
using System.Collections;

namespace SpritePivotEditor{

	public class SpriteAlignmentEditor : EditorWindow {

		private SpriteAlignment spriteAlignment;
		private Vector2 		customPivot;
		private bool			loopThroughAll;
		public bool				canLoopThrough;
		private Vector2			windowSize = new Vector2(250, 50);
		//--------------------------------------------------
		
		
		//--------------------------------------------------
		void OnEnable(){

			//reset
			spriteAlignment = SpriteAlignment.Center;
			loopThroughAll = false;
			customPivot = Vector2.zero;

			minSize = windowSize;

		}
		//--------------------------------------------------
		
		
		//--------------------------------------------------
		void OnInspectorUpdate(){

			Repaint();
		}
		//--------------------------------------------------
		
		
		//--------------------------------------------------
		void OnGUI(){


			GUILayout.BeginVertical();

			spriteAlignment = (SpriteAlignment) EditorGUILayout.EnumPopup("Alignment", spriteAlignment);

			if(spriteAlignment == SpriteAlignment.Custom){


				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				customPivot = EditorGUILayout.Vector2Field("Custom", customPivot, GUILayout.Width(200));
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}


			if(canLoopThrough){

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				loopThroughAll = GUILayout.Toggle(loopThroughAll, "Loop Through All After");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

			}

			GUILayout.BeginHorizontal();
			
			GUILayout.FlexibleSpace();

			if(GUILayout.Button("Cancel")){
				
				Close();
			}
			
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Set")){


				SpritePivotEditor.window.SendEvent(EditorGUIUtility.CommandEvent("SpriteAlignmentSelected"));
				Close();
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();

			Rect windowSizeRect = GUILayoutUtility.GetLastRect();

			if(Event.current.type == EventType.Repaint){

				windowSize.y = windowSizeRect.height;
				windowSize.y += 10;
			}

			//resize window
			minSize = windowSize;
			maxSize = windowSize;
			
			Repaint();
		}
		//--------------------------------------------------
		
		
		//--------------------------------------------------
		public SpriteAlignment	GetSelectedSpriteAlignment(){
			
			return spriteAlignment;
		}
		//--------------------------------------------------
		
		
		//--------------------------------------------------
		public Vector2	GetCustomPivot(){
			
			return customPivot;
		}
		//--------------------------------------------------
		
		
		//--------------------------------------------------
		public bool LoopThroughAll{
			
			
			get{
				
				return loopThroughAll;
			}
		}
	}

}
