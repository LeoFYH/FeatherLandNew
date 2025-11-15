using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Reflection;

namespace SpritePivotEditor{

	public class SpritePivotEditor : EditorWindow {
		
		private int 			selectedAnimationClipIndex;
		private int	 			selectedSpriteIndex;
		private bool			autoSet;
		private bool			showPrevious;
		private bool			isPlaying;
		private bool			showNext;
		private float			sampleTime;
		private bool			isPivotModeEnabled;
		private Sprite			originalSprite;
		private SpriteRenderer 	spriteRenderer;
		private	bool			resetSpriteToOriginalSprite;
		private HideFlags		originalHideFlag;
		private GameObject		selectedGo;
		private bool			resetToOriginalHideFlag;
		private Vector2			pivot;
		private bool			updateSprite;
		private Sprite			spriteToUpdateWith;
		private AnimationClip 	sampleAnimationClip;
		private Sprite			originalSpriteToUpdateWith;
		private SpriteRenderer	previousSpriteFrame;
		private SpriteRenderer	nextSpriteFame;
		private static Color	previousFrameColor = Color.red;
		private static Color	nextFrameColor = Color.blue;
		private static bool		isPrefsLoaded;
		private float			previousTime;
		private bool			reinitialize;
		private SpriteAlignmentEditor	spriteAlignmentEditor;
		public static SpritePivotEditor	window;
		private bool			canSetThroughAllAfter;
		private bool			isPivotDirty;
		private bool			canJumpToFrame;
		private bool			onMouseDownSprite;
		private Sprite			sprite;
		private bool			isInAnimationMode;
		private List<Sprite>	spritesToSaveChangesTo = new List<Sprite>();
		private bool			canUpdatePreviousFrame;
		private bool			canUpdateNextFrame;
		private bool			isKeyDown;
		private static float	step = 0.1f;
		private Vector2			povitToPaste;
		private	Tool			previousTool;
		private Vector3			dragOffset;
		private bool			isSceneFiltered;
		private Vector2			pivotSetToThrough;
		private bool			customPivotLoopThrough;
		private SceneView		currentSceneView;
		private Vector2			windowSize = new Vector2(250, 50);
		private PropertyModification[]	propertyModification;
		//---------------------------------------
		
		
		//---------------------------------------
		[PreferenceItem("Sprite Pivot Editor")]
		static void SpritePivotEditorPreferences()
		{
			if(!isPrefsLoaded){

				LoadColor();

				step = EditorPrefs.GetFloat("SPE_Step", 0.1f);
				isPrefsLoaded = true;
			}

			previousFrameColor = EditorGUILayout.ColorField ("Previous Frame Color", previousFrameColor);

			nextFrameColor = EditorGUILayout.ColorField ("Next Frame Color", nextFrameColor);

			step = EditorGUILayout.FloatField("Step", step);

			if(GUI.changed){


				SaveColor();

				EditorPrefs.SetFloat("SPE_Step", step);
			}
		}
		//---------------------------------------
		
		
		//---------------------------------------
		[MenuItem("GameObject/Sprite Pivot Editor #p")]
		static void Init(){

			window = (SpritePivotEditor) GetWindow<SpritePivotEditor> (true, "Sprite Pivot Editor", false);
			//window.maxSize = new Vector2 (250, 107.5f);
			//window.maxSize = new Vector2 (250, 142f);
		}
		//---------------------------------------
		
		
		//---------------------------------------
		void OnEnable(){

			LoadColor ();
			step = EditorPrefs.GetFloat("SPE_Step", 0.1f);

			sampleAnimationClip = new AnimationClip();
			sampleAnimationClip.hideFlags = HideFlags.DontSave;

			GameObject previousFrameGO = new GameObject ("PreviousFrame");
			previousFrameGO.hideFlags = HideFlags.HideAndDontSave;

			previousSpriteFrame = previousFrameGO.AddComponent<SpriteRenderer>();

			GameObject nextFameGo = new GameObject ("NextFrame");
			nextFameGo.hideFlags = HideFlags.HideAndDontSave;

			nextSpriteFame = nextFameGo.AddComponent<SpriteRenderer>();

			SceneView.onSceneGUIDelegate += OnSceneGUI;


			autoRepaintOnSceneChange = true;

			minSize = windowSize;

		}
		//---------------------------------------
		
		
		//---------------------------------------
		void OnDisable(){

			ResetData();

			//clean up scene
			DestroyImmediate(previousSpriteFrame.gameObject);
			DestroyImmediate(nextSpriteFame.gameObject);

			SceneView.onSceneGUIDelegate -= OnSceneGUI;


			if(spritesToSaveChangesTo != null){
				
				if(spritesToSaveChangesTo.Count > 0){

					ApplyChanges();
					
				}
				
			}


		}
		//---------------------------------------
		
		
		//---------------------------------------
		void OnHierarchyChange(){

			Repaint();
		}
		//---------------------------------------
		
		
		//---------------------------------------
		void OnSelectionChange(){

			ResetData();

			showPrevious = false;
			showNext = false;
			autoSet = false;
			
			previousSpriteFrame.gameObject.SetActive(false);
			nextSpriteFame.gameObject.SetActive(false);


			if(spritesToSaveChangesTo != null){
				
				if(spritesToSaveChangesTo.Count > 0){
					
					ApplyChanges();

					spritesToSaveChangesTo.Clear();

				}
				
			}



		}
		//---------------------------------------
		
		
		//---------------------------------------
		void ResetData(){


			if(isSceneFiltered){

#if UNITY_2019_1_OR_NEWER
				ResetFilter();
#else

				currentSceneView.SetSceneViewFiltering(false);
#endif


			}

			sampleTime = 0;
			selectedAnimationClipIndex = 0;
			selectedSpriteIndex = 0;
			pivot = Vector2.zero;
			isSceneFiltered = false;

			if(resetSpriteToOriginalSprite){
				
				spriteRenderer.sprite = originalSprite;
				resetSpriteToOriginalSprite = false;
				
			}
			
			if(resetToOriginalHideFlag){
				
				selectedGo.hideFlags = originalHideFlag;
				EditorUtility.SetDirty (selectedGo);
				resetToOriginalHideFlag = false;
			}
			
			isPivotModeEnabled = false;
			isPlaying = false;
			Tools.current = previousTool;
			
			Repaint();
		}
		//---------------------------------------
		
		
		//---------------------------------------
		void OnInspectorUpdate(){


			if(isPlaying){

				float deltaTime = (float)EditorApplication.timeSinceStartup - previousTime;
				previousTime = (float)EditorApplication.timeSinceStartup;
				
				sampleTime += deltaTime;
				
				sampleTime  %= sampleAnimationClip.length;

				SceneView.RepaintAll();
			
			}



			Repaint();
		}
		//---------------------------------------
		
		
		//---------------------------------------
		void OnGUI(){

			if(EditorApplication.isPlayingOrWillChangePlaymode){

				if(isPivotModeEnabled){

					ResetData();
				}
				                   

				GUILayout.BeginVertical();
				GUILayout.FlexibleSpace();

				EditorGUILayout.HelpBox("Can't Use This In Play Mode", MessageType.Info);
				GUILayout.FlexibleSpace();


				GUILayout.EndVertical();

				return;
			}

			//get selected gameobject
			selectedGo = Selection.activeGameObject;

			if(selectedGo != null){
				
				//check if has animator component
				Animator animator = selectedGo.GetComponent<Animator>();

				if(animator != null){

					if(isPivotModeEnabled){
						
						if(Tools.current != Tool.View){
							Tools.current = Tool.View;
						}

					}
					//check if it actually has sprite animation
					bool hasSpriteAnimation = false;
					
					AnimationClip[] animationClips = AnimationUtility.GetAnimationClips(selectedGo);

					List<AnimationClip> spriteAnimationClips = new List<AnimationClip>();

					List<EditorCurveBinding> editorCurveBindings = new List<EditorCurveBinding>();

					List<string> popupDisplayOptions = new List<string>();

					for(int i = 0; i < animationClips.Length; i++){

						AnimationClip clip = animationClips[i];

						EditorCurveBinding[] binding = AnimationUtility.GetObjectReferenceCurveBindings(clip);
						
						for(int j= 0; j < binding.Length; j++){
							
							if(binding[j].type == typeof(SpriteRenderer)){


								//separate sprite animation from the rest
								if(!spriteAnimationClips.Contains(clip)){

									spriteAnimationClips.Add(clip);
									editorCurveBindings.Add(binding[j]);
									popupDisplayOptions.Add(clip.name);
									
								}

							
								hasSpriteAnimation = true;

								break;

							}
						}
						
					}
					
					if(hasSpriteAnimation){

						//Show UI for dealing with sprite animation

						EditorGUI.BeginDisabledGroup(!isPivotModeEnabled);
						GUILayout.BeginVertical();

						EditorGUIUtility.labelWidth = 100;

						EditorGUI.BeginChangeCheck();

						int selectedAnimationClipIndexTmp = EditorGUILayout.Popup ("Animation Clip ",selectedAnimationClipIndex, popupDisplayOptions.ToArray(), GUILayout.Width(240));

						if(EditorGUI.EndChangeCheck()){

							if(selectedAnimationClipIndex != selectedAnimationClipIndexTmp){

								selectedAnimationClipIndex = selectedAnimationClipIndexTmp;
								selectedSpriteIndex = 0;
								sampleTime = 0;

								reinitialize = true;
							}
						}

						AnimationClip selecteSpriteAnimationClip = spriteAnimationClips[selectedAnimationClipIndex];

						EditorCurveBinding selectedEditorCurveBinding = editorCurveBindings[selectedAnimationClipIndex];

					
						ObjectReferenceKeyframe[] spriteKeyframes =  AnimationUtility.GetObjectReferenceCurve(selecteSpriteAnimationClip, selectedEditorCurveBinding);

						if(canSetThroughAllAfter){

							int i = selectedSpriteIndex + 1;


							for(; i < spriteKeyframes.Length; i++){

								Sprite spriteTmp = (Sprite) spriteKeyframes[i].value;

								if(spriteTmp != null){

									//SetSpritePivot(spriteTmp, ConvertPivotBack(pivotSetToThrough, spriteTmp));
									if(!customPivotLoopThrough){

										SetSpritePivot(spriteTmp,pivotSetToThrough);

									}
									else{


										SetSpritePivot(spriteTmp, ConvertPivotBack(pivotSetToThrough, spriteTmp));



									}

									/*if(!customPivotLoopThrough){
									 
										SetSpritePivot(spriteTmp, ConvertPivotBack(pivot, spriteTmp));


									}
									else{

										//Vector2 tmp = (pivot);
										//convert to texture space
										//tmp.x /= spriteTmp.bounds.size.x;
										//tmp.y /= spriteTmp.bounds.size.y;



										SetSpritePivot(spriteTmp, ConvertPivotBack(pivot, spriteTmp));

									}*/
								}


									
							}


							customPivotLoopThrough = false;
							canSetThroughAllAfter = false;
						}
						spriteRenderer = (SpriteRenderer) AnimationUtility.GetAnimatedObject(selectedGo, selectedEditorCurveBinding);

						if(spriteRenderer ==null){
							
							EditorGUI.HelpBox (new Rect (position.width/2 - 175/2, position.height/2 - 50/2, 175, 50), "Missing Sprite Renderer in animation", MessageType.Info);
						
							return;
							
						}

						if(reinitialize){

							GUI.FocusControl(null);
							Sprite spriteTmp = (Sprite) spriteKeyframes[selectedSpriteIndex].value;


							originalSpriteToUpdateWith = spriteTmp;
							
							if(spriteTmp != null){
								
								pivot = GetSpritePivot(spriteTmp);
								
								//duplicate sprite and work from that only
								string spriteName = spriteTmp.name;
								spriteToUpdateWith  = Sprite.Create(spriteTmp.texture, spriteTmp.rect, pivot, spriteTmp.pixelsPerUnit);
								spriteToUpdateWith.hideFlags = HideFlags.DontSave;
								spriteToUpdateWith.name = spriteName;
								
								updateSprite = true;
								
								pivot = ConvertPivot(pivot, spriteTmp);


								
							}else{

								pivot = Vector2.zero;
								spriteToUpdateWith = null;
							}

							canUpdatePreviousFrame = true;
							canUpdateNextFrame = true;
							reinitialize = false;

						}

						//update sprite in keyframes
						if(updateSprite){

							spriteKeyframes[selectedSpriteIndex].value = spriteToUpdateWith;
							AnimationUtility.SetObjectReferenceCurve(sampleAnimationClip, selectedEditorCurveBinding, spriteKeyframes);
							updateSprite = false;
						}

						//update previous frame
						if(showPrevious){

							if(selectedSpriteIndex > 0){

								if(canUpdatePreviousFrame){

									previousSpriteFrame.transform.position = spriteRenderer.transform.position;

									if(!previousSpriteFrame.gameObject.activeSelf)
										previousSpriteFrame.gameObject.SetActive(true);

									Sprite previousSprite = (Sprite) spriteKeyframes[selectedSpriteIndex -1].value;

										
									if(previousSpriteFrame.sprite != previousSprite){

										if(previousSprite != null){

											Vector2 previousSpritePivot = GetSpritePivot(previousSprite);
							
											string previousSpriteName = previousSprite.name;
											previousSprite = Sprite.Create(previousSprite.texture, previousSprite.rect, previousSpritePivot, previousSprite.pixelsPerUnit);
											previousSprite.hideFlags = HideFlags.DontSave;
											previousSprite.name = previousSpriteName;
										}

										previousSpriteFrame.sprite = previousSprite;
									}

									

									if(previousSpriteFrame.sharedMaterial != spriteRenderer.sharedMaterial){

										previousSpriteFrame.sharedMaterial = spriteRenderer.sharedMaterial;
									}

									if(previousSpriteFrame.color != previousFrameColor){

										previousSpriteFrame.color = previousFrameColor;
									}


									if(previousSpriteFrame.sortingLayerID != spriteRenderer.sortingLayerID){

										previousSpriteFrame.sortingLayerID = spriteRenderer.sortingLayerID;
									}

									if(previousSpriteFrame.sortingOrder >= spriteRenderer.sortingOrder){

										previousSpriteFrame.sortingOrder = spriteRenderer.sortingOrder - 10;
									}

									canUpdatePreviousFrame = false;
								}
							}
							else{

								if(previousSpriteFrame.gameObject.activeSelf)
									previousSpriteFrame.gameObject.SetActive(false);

							}
						}
						else{

							if(previousSpriteFrame.gameObject.activeSelf)
								previousSpriteFrame.gameObject.SetActive(false);
						}

						//update next frame
						if(showNext){

							if(selectedSpriteIndex < spriteKeyframes.Length - 1){

								if(canUpdateNextFrame){

									nextSpriteFame.transform.position = spriteRenderer.transform.position;

									if(!nextSpriteFame.gameObject.activeSelf)
										nextSpriteFame.gameObject.SetActive(true);

									Sprite nextSprite = (Sprite) spriteKeyframes[selectedSpriteIndex + 1].value;
									
									if(nextSpriteFame.sprite != nextSprite){

										if(nextSprite != null){

											Vector2 nextSpritePivot = GetSpritePivot(nextSprite);

											string nextSpriteName = nextSprite.name;
											nextSprite = Sprite.Create(nextSprite.texture, nextSprite.rect, nextSpritePivot, nextSprite.pixelsPerUnit);
											nextSprite.hideFlags = HideFlags.DontSave;
											nextSprite.name = nextSpriteName;
										}
										nextSpriteFame.sprite = nextSprite;
									}

									if(nextSpriteFame.sharedMaterial != nextSpriteFame.sharedMaterial){
										
										nextSpriteFame.sharedMaterial = nextSpriteFame.sharedMaterial;
									}

									if(nextSpriteFame.color != nextFrameColor){
										
										nextSpriteFame.color = nextFrameColor;
									}

									if(nextSpriteFame.sortingLayerID != spriteRenderer.sortingLayerID){
										
										nextSpriteFame.sortingLayerID = spriteRenderer.sortingLayerID;
									}

									if(nextSpriteFame.sortingOrder <= spriteRenderer.sortingOrder){
										
										nextSpriteFame.sortingOrder = spriteRenderer.sortingOrder + 10;
									}

								}
									
							}
							else{
								
								if(nextSpriteFame.gameObject.activeSelf)
									nextSpriteFame.gameObject.SetActive(false);
							}

						}
						else{

							if(nextSpriteFame.gameObject.activeSelf)
								nextSpriteFame.gameObject.SetActive(false);
						}

						//sample the animation clip

						if(isPivotModeEnabled){


							sampleAnimationClip.SampleAnimation(selectedGo, sampleTime);
						}
						
					
						sprite = spriteRenderer.sprite;


						if(isPlaying){
							//update selected sprite Index according to animation
							for(int i = 0; i < spriteKeyframes.Length; i++){

								if(spriteKeyframes[i].value == sprite){

									if(selectedSpriteIndex != i){

										selectedSpriteIndex = i;
										reinitialize = true;
									}
								}
							}

						}



						if(sprite != null){



							GUILayout.BeginHorizontal();
							GUILayout.FlexibleSpace();
							GUILayout.Label(sprite.name, GUI.skin.box);
							GUILayout.FlexibleSpace();
							GUILayout.EndHorizontal();

							if(!canJumpToFrame){

								string label = "(" + (selectedSpriteIndex) + "  /  " +  (spriteKeyframes.Length - 1) + ")";

								GUILayout.BeginHorizontal();
								GUILayout.FlexibleSpace();
								GUILayout.Label(label);
								Rect lastRect = GUILayoutUtility.GetLastRect();
								GUILayout.FlexibleSpace();
								GUILayout.EndHorizontal();


								if(lastRect.Contains(Event.current.mousePosition)){

									if(Event.current.type == EventType.MouseDown && Event.current.clickCount == 2){

										canJumpToFrame = true;
										//GUI.FocusControl("JumpToFrame");
										GUI.FocusControl(null);
									}
								}
							}
							else{

								GUILayout.BeginHorizontal();
								GUILayout.FlexibleSpace();

								GUIStyle label = GUI.skin.GetStyle("Label");
								label.alignment = TextAnchor.MiddleCenter;

								GUI.SetNextControlName("JumpToFrame");
								selectedSpriteIndex = EditorGUILayout.IntField(selectedSpriteIndex, label, GUILayout.Width(100));
								selectedSpriteIndex = Mathf.Clamp(selectedSpriteIndex, 0, spriteKeyframes.Length -1);
								Rect lastRect = GUILayoutUtility.GetLastRect();
								GUILayout.FlexibleSpace();
								GUILayout.EndHorizontal();

								if(Event.current.Equals(Event.KeyboardEvent("return")) && GUI.GetNameOfFocusedControl() == "JumpToFrame"){

									GUI.FocusControl(null);
									sampleTime = spriteKeyframes[selectedSpriteIndex].time;
									reinitialize = true;
									canJumpToFrame = false;
									Event.current.Use();

								}

								if(!lastRect.Contains(Event.current.mousePosition)){

									if(Event.current.type == EventType.MouseDown){

										GUI.FocusControl(null);
										sampleTime = spriteKeyframes[selectedSpriteIndex].time;
										reinitialize = true;
										canJumpToFrame = false;
									}
								}
								//GUI.SetNextControlName("");
							}
						}
						else{


							EditorGUILayout.HelpBox("Sprite is null", MessageType.Info);
						}



						GUILayout.BeginHorizontal();
					
						EditorGUI.BeginDisabledGroup(sprite == null || isPlaying);

						EditorGUI.BeginChangeCheck();


						pivot =  EditorGUILayout.Vector2Field("Pivot", pivot, GUILayout.Width(125));
						MoveWithKeyBoard();
						if(EditorGUI.EndChangeCheck()){

							pivot.x = Mathf.Round(pivot.x * 1000) /1000;
							pivot.y = Mathf.Round(pivot.y * 1000) /1000;

							//update sprite with updated pivot
							string spriteName = sprite.name;
							spriteToUpdateWith  = Sprite.Create(sprite.texture, sprite.rect, ConvertPivotBack(pivot, sprite), sprite.pixelsPerUnit);
							spriteToUpdateWith.hideFlags = HideFlags.DontSave;
							spriteToUpdateWith.name = spriteName;
							
							updateSprite = true;

							if(autoSet){

								if(!isPivotDirty)
									isPivotDirty = true;

							}

						}

						if(!onMouseDownSprite){

							if(isPivotDirty){

								if(GUIUtility.hotControl <= 0){

									SetSpritePivot(originalSpriteToUpdateWith,ConvertPivotBack(pivot, originalSpriteToUpdateWith));


									isPivotDirty = false;
								}

							}
						}



						GUILayout.BeginVertical();
						GUILayout.Space(12.5f);
						
						GUILayout.BeginHorizontal();
						
						if(GUILayout.Button("A", GUILayout.Height(25f))){

							if(spriteAlignmentEditor == null){
								spriteAlignmentEditor = (SpriteAlignmentEditor) EditorWindow.GetWindow<SpriteAlignmentEditor>(true, "Sprite Alignment Editor", false);


							}
							else{

								spriteAlignmentEditor.Show();
							}

							spriteAlignmentEditor.canLoopThrough = true;

							Rect spriteAlignmentEditorRect = spriteAlignmentEditor.position;
							spriteAlignmentEditorRect.center = position.center;
							spriteAlignmentEditor.position = spriteAlignmentEditorRect;


						}
						
						if(GUILayout.Button("Set", GUILayout.Height(25f))){
							

							SetSpritePivot(originalSpriteToUpdateWith,ConvertPivotBack(pivot, originalSpriteToUpdateWith));
						}

						EditorGUI.EndDisabledGroup();
						
						autoSet = GUILayout.Toggle(autoSet, "Auto", GUI.skin.button, GUILayout.Height(25f));
						
						
						GUILayout.EndHorizontal();

						GUILayout.EndVertical();
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();

						EditorGUI.BeginChangeCheck();
						showPrevious = GUILayout.Toggle(showPrevious, "O|", GUI.skin.button);
						if(EditorGUI.EndChangeCheck()){

							if(showPrevious)
								canUpdatePreviousFrame = true;
						}
						
						if(GUILayout.Button("<")){
							

							selectedSpriteIndex--;
							
							if(selectedSpriteIndex < 0){
								selectedSpriteIndex = 0;
								
							}


							sampleTime = spriteKeyframes[selectedSpriteIndex].time;

							reinitialize = true;
							
						}
						
						if(!isPlaying){
							
							if(GUILayout.Button("Play")){
								
								isPlaying = true;
								previousTime = (float)EditorApplication.timeSinceStartup;


							}
						}
						else{
							
							if(GUILayout.Button("Stop")){
								
								isPlaying = false;


							}
						}
						
						if(GUILayout.Button(">")){
							
							selectedSpriteIndex++;
							
							if(selectedSpriteIndex >= spriteKeyframes.Length){
								
								selectedSpriteIndex = spriteKeyframes.Length - 1;
							}

							sampleTime = spriteKeyframes[selectedSpriteIndex].time;

							reinitialize = true;

						}

						EditorGUI.BeginChangeCheck();
						showNext = GUILayout.Toggle(showNext, "|O", GUI.skin.button);
						if(EditorGUI.EndChangeCheck()){

							if(showNext)
								canUpdateNextFrame = true;
						}
						
						GUILayout.EndHorizontal();


						if(GUILayout.Button("Disable Pivot\nMode")){

							HierarchyProperty.FilterSingleSceneObject(selectedGo.GetInstanceID(), true);

							if(isSceneFiltered){

#if UNITY_2019_1_OR_NEWER
								ResetFilter();
#else
								currentSceneView.SetSceneViewFiltering(false);
#endif



								isSceneFiltered = false;
							}

							//reset
							sampleTime = 0;
							selectedAnimationClipIndex = 0;
							selectedSpriteIndex = 0;
							pivot = Vector2.zero;
							spriteRenderer.sprite = originalSprite;
							showPrevious = false;
							showNext = false;
							autoSet = false;
							previousSpriteFrame.gameObject.SetActive(false);
							nextSpriteFame.gameObject.SetActive(false);
							resetSpriteToOriginalSprite = false;
							selectedGo.hideFlags = originalHideFlag;
							EditorUtility.SetDirty(selectedGo);
							resetToOriginalHideFlag = false;
							isPivotModeEnabled = false;
							Tools.current = previousTool;
							isPlaying = false;

							if(spritesToSaveChangesTo != null){
								
								if(spritesToSaveChangesTo.Count > 0){
									
									ApplyChanges();
									spritesToSaveChangesTo.Clear();
									
								}
								
							}

							PrefabUtility.SetPropertyModifications(selectedGo, propertyModification);
						}
						GUILayout.EndVertical();

						Rect windowSizeRect = GUILayoutUtility.GetLastRect();

						if(Event.current.type == EventType.Repaint){

							windowSize.y = windowSizeRect.height;
							windowSize.y += 5;
						}

						//resize window
						minSize = windowSize;
						maxSize = windowSize;

						EditorGUI.EndDisabledGroup();

						if(!isPivotModeEnabled){
							
							GUILayout.BeginArea(new Rect(0, 0, position.width, position.height));
							
							GUILayout.FlexibleSpace();
							
							GUILayout.BeginHorizontal();
							
							GUILayout.FlexibleSpace();
							
							if(GUILayout.Button("Enable Pivot\nMonde")){

								HierarchyProperty.FilterSingleSceneObject(selectedGo.GetInstanceID(), false);

								if(currentSceneView != null){

#if UNITY_2019_1_OR_NEWER
									FilterScene();
#else

									currentSceneView.SetSceneViewFiltering(true);

#endif


									isSceneFiltered = true;
								}
								else{

									isSceneFiltered = false;
								}

								originalSprite = sprite;
								resetSpriteToOriginalSprite = true;
								originalHideFlag = selectedGo.hideFlags;
								selectedGo.hideFlags = HideFlags.NotEditable;
								EditorUtility.SetDirty(selectedGo);
								resetToOriginalHideFlag = true;
								isPivotModeEnabled = true;

								reinitialize = true;

								if(spriteAlignmentEditor != null)
									spriteAlignmentEditor.canLoopThrough = true;

								isInAnimationMode = true;
								previousTool = Tools.current;


								Tools.current = Tool.View;
								propertyModification = PrefabUtility.GetPropertyModifications(selectedGo);
							}
							
							GUILayout.FlexibleSpace();
							
							GUILayout.EndHorizontal();
							
							
							GUILayout.FlexibleSpace();
							
							GUILayout.EndArea();
						}

						if(Event.current.type == EventType.ValidateCommand && Event.current.commandName == "SpriteAlignmentSelected"){
							
							Event.current.Use();
						}
						
						if(Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "SpriteAlignmentSelected"){
							
							SpriteAlignment selectedSpriteAlignment = spriteAlignmentEditor.GetSelectedSpriteAlignment();
							
							if(selectedSpriteAlignment != SpriteAlignment.Custom){
								
								pivot = CalPivotFromAlignment(selectedSpriteAlignment);
								pivotSetToThrough = pivot;
								pivot = ConvertPivot(pivot, sprite);
							}
							else{
								
								pivot = spriteAlignmentEditor.GetCustomPivot();
								pivotSetToThrough = pivot;


								customPivotLoopThrough = true;


							}

							//update sprite with updated pivot
							string spriteName = sprite.name;
							spriteToUpdateWith  = Sprite.Create(sprite.texture, sprite.rect, ConvertPivotBack(pivot, sprite), sprite.pixelsPerUnit);
							spriteToUpdateWith.hideFlags = HideFlags.DontSave;
							spriteToUpdateWith.name = spriteName;
							
							updateSprite = true;

							SetSpritePivot(originalSpriteToUpdateWith,ConvertPivotBack(pivot, originalSpriteToUpdateWith));


							if(spriteAlignmentEditor.LoopThroughAll){

								canSetThroughAllAfter = true;

							}

							Focus();
							
						}


						if(Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Copy"){

							GUI.FocusControl(null);
							Event.current.Use();
						}
						
						if(Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Copy"){

							povitToPaste = pivot;
						}

						if(Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Paste"){


							Event.current.Use();
						}
						
						if(Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Paste"){


							pivot = povitToPaste;

							//update sprite with updated pivot
							string spriteName = sprite.name;
							spriteToUpdateWith  = Sprite.Create(sprite.texture, sprite.rect, ConvertPivotBack(pivot, sprite), sprite.pixelsPerUnit);
							spriteToUpdateWith.hideFlags = HideFlags.DontSave;
							spriteToUpdateWith.name = spriteName;
							
							updateSprite = true;
							
							if(autoSet){
								
								if(!isPivotDirty)
									isPivotDirty = true;
								
							}


						}

						
						return;
					}
				}

				//check if has sprite renderer component
				spriteRenderer = selectedGo.GetComponent<SpriteRenderer>();

				if(spriteRenderer != null){

					if(isPivotModeEnabled){
						
						if(Tools.current != Tool.View){
							Tools.current = Tool.View;
						}


					}
				
					//if so show UI for dealing with an single sprite

					sprite = spriteRenderer.sprite;

					//have to make sure sprite is not null
					if(sprite != null){


						EditorGUI.BeginDisabledGroup(!isPivotModeEnabled);
						GUILayout.BeginVertical();


						GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						GUILayout.Label(sprite.name, GUI.skin.box);
						GUILayout.FlexibleSpace();
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();

						EditorGUI.BeginChangeCheck();




						pivot = EditorGUILayout.Vector2Field("Pivot", pivot, GUILayout.Width(125));

						MoveWithKeyBoard();

						if(EditorGUI.EndChangeCheck()){

							pivot.x = Mathf.Round(pivot.x * 1000) /1000;
							pivot.y = Mathf.Round(pivot.y * 1000) /1000;
							


							//update sprite with updated pivot
							string spriteName = sprite.name;
							sprite = Sprite.Create(sprite.texture, sprite.rect, ConvertPivotBack(pivot, sprite), sprite.pixelsPerUnit);
							sprite.hideFlags = HideFlags.DontSave;
							sprite.name = spriteName;

							//update sprite renderer with recently updated duplicate
							spriteRenderer.sprite = sprite;


						}

						GUILayout.BeginVertical();
						GUILayout.Space(12.5f);
						
						GUILayout.BeginHorizontal();
						
						if(GUILayout.Button("A", GUILayout.Height(25f))){
							
							if(spriteAlignmentEditor == null){
								spriteAlignmentEditor = (SpriteAlignmentEditor) EditorWindow.GetWindow<SpriteAlignmentEditor>(true, "Sprite Alignment Editor", false);
							}
							else{
								
								spriteAlignmentEditor.Show();
							}

							spriteAlignmentEditor.canLoopThrough = false;

							Rect spriteAlignmentEditorRect = spriteAlignmentEditor.position;
							spriteAlignmentEditorRect.center = position.center;
							spriteAlignmentEditor.position = spriteAlignmentEditorRect;
							


						}
						
						if(GUILayout.Button("Set", GUILayout.Height(25f))){
							
							
							SetSpritePivot(originalSprite,ConvertPivotBack(pivot, originalSprite));
						}

						
						GUILayout.EndHorizontal();
						
						GUILayout.EndVertical();
						GUILayout.EndHorizontal();


						if(GUILayout.Button("Disable Pivot\nMode")){

							HierarchyProperty.FilterSingleSceneObject(selectedGo.GetInstanceID(), true);

							if(isSceneFiltered){

#if UNITY_2019_1_OR_NEWER
								ResetFilter();
#else
								currentSceneView.SetSceneViewFiltering(false);

#endif





								isSceneFiltered = false;
							}

							pivot = Vector2.zero;
							spriteRenderer.sprite = originalSprite;
							resetSpriteToOriginalSprite = false;
							selectedGo.hideFlags = originalHideFlag;
							EditorUtility.SetDirty(selectedGo);
							resetToOriginalHideFlag = false;
							isPivotModeEnabled = false;


							if(spritesToSaveChangesTo != null){
								
								if(spritesToSaveChangesTo.Count > 0){
									
									ApplyChanges();
									spritesToSaveChangesTo.Clear();
									
								}
								
							}

							Tools.current = previousTool;
							PrefabUtility.SetPropertyModifications(selectedGo, propertyModification);
						}

						GUILayout.EndVertical();

						Rect windowSizeRect = GUILayoutUtility.GetLastRect();

						if(Event.current.type == EventType.Repaint){

							windowSize.y = windowSizeRect.height;
							windowSize.y += 10;
						}

						//resize window
						maxSize = windowSize;
						minSize = windowSize;

						EditorGUI.EndDisabledGroup();

						if(!isPivotModeEnabled){

							GUILayout.BeginArea(new Rect(0, 0, position.width, position.height));

							GUILayout.FlexibleSpace();

							GUILayout.BeginHorizontal();

							GUILayout.FlexibleSpace();

							if(GUILayout.Button("Enable Pivot\nMonde")){

								HierarchyProperty.FilterSingleSceneObject(selectedGo.GetInstanceID(), false);

								if(currentSceneView != null){

#if UNITY_2019_1_OR_NEWER
									FilterScene();
#else
									currentSceneView.SetSceneViewFiltering(true);

#endif


									isSceneFiltered = true;
								}
								else{

									isSceneFiltered = false;
								}

								originalSprite = sprite;
								resetSpriteToOriginalSprite = true;
								originalHideFlag = selectedGo.hideFlags;
								selectedGo.hideFlags = HideFlags.NotEditable;
								EditorUtility.SetDirty(selectedGo);
								resetToOriginalHideFlag = true;
								isPivotModeEnabled = true;

								pivot = GetSpritePivot(sprite);

								//duplicate sprite and work from that only
								string spriteName = sprite.name;
								sprite = Sprite.Create(sprite.texture, sprite.rect, pivot, sprite.pixelsPerUnit);
								sprite.hideFlags = HideFlags.DontSave;
								sprite.name = spriteName;

								//update sprite renderer with duplicate
								spriteRenderer.sprite = sprite;



								pivot = ConvertPivot(pivot, sprite);

								if(spriteAlignmentEditor != null)
									spriteAlignmentEditor.canLoopThrough = false;

								isInAnimationMode = false;

								previousTool = Tools.current;

								Tools.current = Tool.View;
								propertyModification = PrefabUtility.GetPropertyModifications(selectedGo);

							}

							GUILayout.FlexibleSpace();

							GUILayout.EndHorizontal();


							GUILayout.FlexibleSpace();

							GUILayout.EndArea();
						}

						if(Event.current.type == EventType.ValidateCommand && Event.current.commandName == "SpriteAlignmentSelected"){

							Event.current.Use();
						}

						if(Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "SpriteAlignmentSelected"){
							
							SpriteAlignment selectedSpriteAlignment = spriteAlignmentEditor.GetSelectedSpriteAlignment();

							if(selectedSpriteAlignment != SpriteAlignment.Custom){


								pivot = CalPivotFromAlignment(selectedSpriteAlignment);

								pivot = ConvertPivot(pivot, sprite);

							}
							else{

								pivot = spriteAlignmentEditor.GetCustomPivot();
							}



							//update sprite with updated pivot
							string spriteName = sprite.name;
							sprite = Sprite.Create(sprite.texture, sprite.rect, ConvertPivotBack(pivot, sprite), sprite.pixelsPerUnit);
							sprite.hideFlags = HideFlags.DontSave;
							sprite.name = spriteName;
							
							//update sprite renderer with recently updated duplicate
							spriteRenderer.sprite = sprite;

							SetSpritePivot(originalSprite,ConvertPivotBack(pivot, originalSprite));

							Focus();

							Repaint();
						}



						return;
					}

					EditorGUI.HelpBox (new Rect (position.width/2 - 175/2, position.height/2 - 50/2, 175, 50), "Missing Sprite from Sprite Component", MessageType.Info);


					return;
				}

			}

			//display message if notthing else
			EditorGUI.HelpBox (new Rect (position.width/2 - 175/2, position.height/2 - 50/2, 175, 50), "Has no Sprites Data to work with", MessageType.Info);
		
		}
		//---------------------------------------
		
		
		//---------------------------------------
		void OnSceneGUI(SceneView sceneView){

			currentSceneView = sceneView;

			if(isPivotModeEnabled){

				if(!isSceneFiltered){

#if UNITY_2019_1_OR_NEWER
					
					FilterScene();

#else
					currentSceneView.SetSceneViewFiltering(true);
#endif

					isSceneFiltered = true;
				}

				if(!isPlaying){

					Vector3 mousePos = MousePos();

					switch(Event.current.type){

					case EventType.MouseDown:

					
						if(spriteRenderer.bounds.Contains(mousePos)){
							
							onMouseDownSprite = true;

							dragOffset = (Vector2)selectedGo.transform.InverseTransformPoint(mousePos) - pivot;
							
					

							Event.current.Use();
						}

						break;

					case EventType.MouseDrag:

						if(onMouseDownSprite){

							Vector3 tmp = selectedGo.transform.InverseTransformPoint(mousePos);

							//convert to texture space
							//tmp.x /= sprite.bounds.size.x;
							//tmp.y /= sprite.bounds.size.y;



							pivot = tmp - dragOffset;

							pivot.x = Mathf.Round(pivot.x * 1000) /1000;
							pivot.y = Mathf.Round(pivot.y * 1000) /1000;


							if(isInAnimationMode){

								//update sprite with updated pivot
								string spriteName = sprite.name;
								spriteToUpdateWith  = Sprite.Create(sprite.texture, sprite.rect, ConvertPivotBack(pivot, sprite), sprite.pixelsPerUnit);
								spriteToUpdateWith.hideFlags = HideFlags.DontSave;
								spriteToUpdateWith.name = spriteName;
								
								updateSprite = true;
								
								if(autoSet){
									
									if(!isPivotDirty)
										isPivotDirty = true;
									
								}


							}
							else{


								//update sprite with updated pivot
								string spriteName = sprite.name;
								sprite = Sprite.Create(sprite.texture, sprite.rect,ConvertPivotBack(pivot, sprite), sprite.pixelsPerUnit);
								sprite.hideFlags = HideFlags.DontSave;
								sprite.name = spriteName;


								//update sprite renderer with recently updated duplicate
								spriteRenderer.sprite = sprite;

							}

							SceneView.RepaintAll();
						
							Event.current.Use();
						}

						break;

					case EventType.MouseUp:

						if(onMouseDownSprite){
							

							if(isInAnimationMode){

								if(isPivotDirty){
									
									if(GUIUtility.hotControl <= 0){
										
										SetSpritePivot(originalSpriteToUpdateWith,ConvertPivotBack(pivot, originalSpriteToUpdateWith));
										
										
										isPivotDirty = false;
									}
									
								}

							}

							onMouseDownSprite = false;
							
						}
						break;

					
					}

					MoveWithKeyBoard();

					if(isInAnimationMode){

						if(Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Copy"){
							
							Event.current.Use();
						}
						
						if(Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Copy"){
							
							povitToPaste = pivot;
						}
						
						if(Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Paste"){
							
							
							Event.current.Use();
						}
						
						if(Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Paste"){
							
							
							pivot = povitToPaste;
							
							//update sprite with updated pivot
							string spriteName = sprite.name;
							spriteToUpdateWith  = Sprite.Create(sprite.texture, sprite.rect, ConvertPivotBack(pivot, sprite), sprite.pixelsPerUnit);
							spriteToUpdateWith.hideFlags = HideFlags.DontSave;
							spriteToUpdateWith.name = spriteName;
							
							updateSprite = true;
							
							if(autoSet){
								
								if(!isPivotDirty)
									isPivotDirty = true;
								
							}
							
							
						}
						

					}


				}


				Handles.PositionHandle(selectedGo.transform.position, Quaternion.identity);

				currentSceneView.Repaint();
			}
		}
		//---------------------------------------
		
		
		//---------------------------------------
		private Vector3 MousePos(){
			
			/*Vector2 mousePos = Event.current.mousePosition;
			SceneView sceneView = SceneView.currentDrawingSceneView;
			mousePos.y = sceneView.camera.pixelHeight - mousePos.y;
			mousePos = sceneView.camera.ScreenToWorldPoint(mousePos);


			
			return mousePos;
			*/
				Vector3 mousePos = Vector2.zero;
				Plane plane = new Plane(selectedGo.transform.forward, selectedGo.transform.position);

				Ray ray = HandleUtility.GUIPointToWorldRay (Event.current.mousePosition);
			
				float rayDistance = 0;
				if(plane.Raycast(ray, out rayDistance)){

					mousePos = ray.GetPoint(rayDistance);
				}
				return mousePos;


		}
		//---------------------------------------
		
		
		//---------------------------------------
		void MoveWithKeyBoard(){

			switch(Event.current.type){
				
			case EventType.KeyDown:
				
				switch(Event.current.keyCode){
					
				case KeyCode.LeftArrow:
					
					if(!isKeyDown){
						
						pivot.x -= step;
						isKeyDown = true;
						pivot.x = Mathf.Round(pivot.x * 1000) /1000;

					}
					
					Event.current.Use();
					break;
					
					
				case KeyCode.UpArrow:
					
					if(!isKeyDown){
						
						pivot.y += step;
						pivot.y = Mathf.Round(pivot.y * 1000) /1000;

						isKeyDown = true;
					}
					
					Event.current.Use();
					break;
					
				case KeyCode.RightArrow:
					
					if(!isKeyDown){
						
						pivot.x += step;
						pivot.x = Mathf.Round(pivot.x * 1000) /1000;

						isKeyDown = true;
					}
					
					Event.current.Use();
					break;
					
				case KeyCode.DownArrow:
					
					if(!isKeyDown){
						
						pivot.y -= step;
						pivot.y = Mathf.Round(pivot.y * 1000) /1000;

						isKeyDown = true;
					}
					
					Event.current.Use();
					
					break;
					
				}
				
				
				break;
				
			case EventType.KeyUp:
				
				if(isKeyDown){
					
					
					if(isInAnimationMode){
						
						//update sprite with updated pivot
						string spriteName = sprite.name;
						spriteToUpdateWith  = Sprite.Create(sprite.texture, sprite.rect, ConvertPivotBack(pivot, sprite), sprite.pixelsPerUnit);
						spriteToUpdateWith.hideFlags = HideFlags.DontSave;
						spriteToUpdateWith.name = spriteName;
						
						updateSprite = true;
						
						if(autoSet){
							
							if(!isPivotDirty)
								isPivotDirty = true;
							
						}
						
						
					}
					else{
						
						
						//update sprite with updated pivot
						string spriteName = sprite.name;
						sprite = Sprite.Create(sprite.texture, sprite.rect,ConvertPivotBack(pivot, sprite), sprite.pixelsPerUnit);
						sprite.hideFlags = HideFlags.DontSave;
						sprite.name = spriteName;
						
						
						//update sprite renderer with recently updated duplicate
						spriteRenderer.sprite = sprite;
						
					}
					
					SceneView.RepaintAll();
					
					Event.current.Use();
					
					
					isKeyDown = false;
				}
				
				break;
			}


		}
		//---------------------------------------
		
		
		//---------------------------------------
		Vector2 GetSpritePivot(Sprite spr){
			
			string path = AssetDatabase.GetAssetPath(spr.GetInstanceID());
			
			TextureImporter textureImporter = (TextureImporter) AssetImporter.GetAtPath(path);

			SpriteAlignment spriteAlignment;
			Vector2 pivotTmp = Vector2.zero;
			
			//get pivot
			switch(textureImporter.spriteImportMode){
				
			case SpriteImportMode.Single:

			
				TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
				
				textureImporter.ReadTextureSettings(textureImporterSettings);

				//bug have to calaclute pivot according to alignment, if sprite alignment is anything else but custom will return wrong pivot point if not doing this
			
				spriteAlignment = (SpriteAlignment)textureImporterSettings.spriteAlignment;

				if(spriteAlignment != SpriteAlignment.Custom){

					pivotTmp = CalPivotFromAlignment(spriteAlignment);
				}
				else{

					pivotTmp = textureImporterSettings.spritePivot;
				}


				break;
				
			case SpriteImportMode.Multiple:
				
				for(int i = 0; i < textureImporter.spritesheet.Length; i++){

					SpriteMetaData spriteMetaData = textureImporter.spritesheet[i];

					if(spriteMetaData.name == spr.name){

					
							pivotTmp = spriteMetaData.pivot;


						break;
					}
				}



				
				break;
				
			}

			/*pivotTmp -= Vector2.one/2;
			pivotTmp *= -1;
			pivotTmp.x *=  spr.bounds.size.x;
			pivotTmp.y *= spr.bounds.size.y;*/

			//pivotTmp.x = Mathf.Round(pivotTmp.x * 100) / 100;
			//pivotTmp.y = Mathf.Round (pivotTmp.y * 100) / 100;
			return pivotTmp;
		}
		//---------------------------------------
		
		
		//---------------------------------------
		void SetSpritePivot(Sprite spr, Vector2 pivot){

			Vector2 pivotTmp = pivot;

			//convert back
			/*pivotTmp.x /= spr.bounds.size.x;
			pivotTmp.y /= spr.bounds.size.y;
			pivotTmp *= -1;
			pivotTmp += Vector2.one/2;*/


			string path = AssetDatabase.GetAssetPath(spr.GetInstanceID());
			
			TextureImporter textureImporter = (TextureImporter) AssetImporter.GetAtPath(path);

			//set pivot
			switch(textureImporter.spriteImportMode){
				
			case SpriteImportMode.Single:
				
				TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
				
				textureImporter.ReadTextureSettings(textureImporterSettings);
				
				textureImporterSettings.spriteAlignment = (int) CalSpriteAlignment(pivotTmp);
				textureImporterSettings.spritePivot = pivotTmp;
				
				textureImporter.SetTextureSettings(textureImporterSettings);

			
				
				break;
				
			case SpriteImportMode.Multiple:
				
				
				SpriteMetaData[] spriteMetaData = textureImporter.spritesheet;
				
				for(int i = 0; i < spriteMetaData.Length; i++){
					
					
					if(spriteMetaData[i].name == spr.name){


						spriteMetaData[i].pivot = pivotTmp;
						spriteMetaData[i].alignment = (int) CalSpriteAlignment(pivotTmp);


					}
				}
				
				textureImporter.spritesheet = spriteMetaData;



				
				break;
			}


			//save changes
			//EditorUtility.SetDirty(spr);
			EditorUtility.SetDirty(textureImporter);


			if(!spritesToSaveChangesTo.Contains(spr)){
				
				spritesToSaveChangesTo.Add(spr);
			}
			//AssetDatabase.WriteImportSettingsIfDirty(path);
			//AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
			//AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			//AssetDatabase.SaveAssets();
			

		}
		//---------------------------------------
		
		
		//---------------------------------------
		Vector2 ConvertPivot(Vector2 value, Sprite spr){

			//convert pivot relative from center
			//value *= -1;
			//value += Vector2.one/2;


			if(spr != null){

				value -= Vector2.one/2;
				value *= -1;

				value.x *=  spr.bounds.size.x;
				value.y *= spr.bounds.size.y;

				value.x = Mathf.Round(value.x * 1000)/1000;
				value.y = Mathf.Round(value.y * 1000)/1000;


			}
		

			return value;
		}
		//---------------------------------------
		
		
		//---------------------------------------
		Vector2 ConvertPivotBack(Vector2 value, Sprite spr){

			if(sprite != null){

				value.x /= spr.bounds.size.x;
				value.y /= spr.bounds.size.y;
				value *= -1;
				value += Vector2.one/2;
			}

			//convert pivot back relative from bottom left

			//value *= -1;
			//value += Vector2.one/2;

			return value;
		}
		//---------------------------------------
		
		
		//---------------------------------------
		SpriteAlignment CalSpriteAlignment(Vector2 pivot){
			
			SpriteAlignment spriteAlignment;
			
			if(Vector2.Equals(pivot, Vector2.zero)){
				
				spriteAlignment = SpriteAlignment.BottomLeft;
			}
			else if(Vector2.Equals(pivot, new Vector2(0.5f, 0))){
				
				spriteAlignment = SpriteAlignment.BottomCenter;
			}
			else if(Vector2.Equals(pivot, new Vector2(1, 0))){
				
				spriteAlignment = SpriteAlignment.BottomRight;
			}
			else if(Vector2.Equals(pivot, new Vector2(0, 0.5f))){
				
				spriteAlignment = SpriteAlignment.LeftCenter;
			}
			else if(Vector2.Equals(pivot, new Vector2(0.5f, 0.5f))){
				
				spriteAlignment = SpriteAlignment.Center;
			}
			else if(Vector2.Equals(pivot, new Vector2(1, 0.5f))){
				
				spriteAlignment = SpriteAlignment.RightCenter;
			}
			else if(Vector2.Equals(pivot, new Vector2(0, 1))){
				
				spriteAlignment = SpriteAlignment.TopLeft;
			}
			else if(Vector2.Equals(pivot, new Vector2(0.5f, 1))){
				
				spriteAlignment = SpriteAlignment.TopCenter;
			}
			else if(Vector2.Equals(pivot, new Vector2(1, 1))){
				
				spriteAlignment = SpriteAlignment.TopRight;
			}
			else{
				
				spriteAlignment = SpriteAlignment.Custom;
			}
			
			return spriteAlignment;
		}
		//---------------------------------------
		
		
		//---------------------------------------
		Vector2 CalPivotFromAlignment(SpriteAlignment spriteAlignment){
			
			Vector2 pivotTmp = Vector2.zero;

			
			switch(spriteAlignment){
				
			case SpriteAlignment.BottomLeft:
				
				pivotTmp = Vector2.zero;
				
				break;
				
			case SpriteAlignment.BottomCenter:
				
				pivotTmp = new Vector2(0.5f, 0);
				break;
				
			case SpriteAlignment.BottomRight:
				
				pivotTmp = new Vector2(1, 0);
				
				
				break;
				
			case SpriteAlignment.LeftCenter:
				
				pivotTmp = new Vector2(0, 0.5f);
				
				break;
				
			case SpriteAlignment.Center:
				
				pivotTmp = new Vector2(0.5f, 0.5f);
				
				break;
				
				
			case SpriteAlignment.RightCenter:
				
				pivotTmp = new Vector2(1, 0.5f);
				
				
				break;
				
			case SpriteAlignment.TopLeft:
				
				pivotTmp = new Vector2(0, 1);
				
				break;
				
			case SpriteAlignment.TopCenter:
				
				pivotTmp = new Vector2(0.5f, 1);
				
				break;
				
				
			case SpriteAlignment.TopRight:
				
				pivotTmp = new Vector2(1, 1);
				
				
				break;

			}
			
			return pivotTmp;
			
		}
		//---------------------------------------
		
		
		//---------------------------------------
		void ApplyChanges(){
			

			AssetDatabase.StartAssetEditing();
			
			for(int i = 0; i < spritesToSaveChangesTo.Count; i++){
				
				Sprite spr = spritesToSaveChangesTo[i];
				
				string path = AssetDatabase.GetAssetPath(spr.GetInstanceID());
				
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
			}
			
			AssetDatabase.StopAssetEditing();


		}
		//---------------------------------------
		
		
		//---------------------------------------
		static void LoadColor(){

			float previousFrameColorColor_r = EditorPrefs.GetFloat("SPE_SpritePivotEditor_previousFrameColorColor_r", 1);
			float previousFrameColorColor_g = EditorPrefs.GetFloat("SPE_previousFrameColorColor_g", 0);
			float previousFrameColorColor_b = EditorPrefs.GetFloat("SPE_previousFrameColorColor_b", 0);
			float previousFrameColorColor_a = EditorPrefs.GetFloat("SPE_previousFrameColorColor_a", 0.5f);
			
			previousFrameColor = new Color(previousFrameColorColor_r, previousFrameColorColor_g, previousFrameColorColor_b, previousFrameColorColor_a);
			
			float nextFrameColorColor_r = EditorPrefs.GetFloat("SPE_nextFrameColorColor_r", 0);
			float nextFrameColorColor_g = EditorPrefs.GetFloat("SPE_nextFrameColorColor_g", 0);
			float nextFrameColorColor_b = EditorPrefs.GetFloat("SPE_nextFrameColorColor_b", 1);
			float nextFrameColorColor_a = EditorPrefs.GetFloat("SPE_nextFrameColorColor_a", 0.5f);
			
			nextFrameColor = new Color(nextFrameColorColor_r, nextFrameColorColor_g, nextFrameColorColor_b, nextFrameColorColor_a);

		}
		//---------------------------------------
		
		
		//---------------------------------------
		static void SaveColor(){

			EditorPrefs.SetFloat("SPE_previousFrameColorColor_r", previousFrameColor.r);
			EditorPrefs.SetFloat("SPE_previousFrameColorColor_g", previousFrameColor.g);
			EditorPrefs.SetFloat("SPE_previousFrameColorColor_b", previousFrameColor.b);
			EditorPrefs.SetFloat("SPE_previousFrameColorColor_a", previousFrameColor.a);
			
			
			EditorPrefs.SetFloat("SPE_nextFrameColorColor_r", nextFrameColor.r);
			EditorPrefs.SetFloat("SPE_nextFrameColorColor_g", nextFrameColor.g);
			EditorPrefs.SetFloat("SPE_nextFrameColorColor_b", nextFrameColor.b);
			EditorPrefs.SetFloat("SPE_nextFrameColorColor_a", nextFrameColor.a);
			

		}


#if UNITY_2019_1_OR_NEWER

		public void FilterScene()
		{
			
			HierarchyProperty.FilterSingleSceneObject(selectedGo.GetInstanceID(), false);

			SetFilter(true);

			isSceneFiltered = true;
		}

		public void ResetFilter()
		{
			if (!isSceneFiltered)
				return;

			HierarchyProperty.ClearSceneObjectsFilter();

			SetFilter(false);

			isSceneFiltered = false;
		}


		private void SetFilter(bool isFil)
		{
			MethodInfo setSearchType = typeof(SceneView).GetMethod("SetSceneViewFiltering", BindingFlags.NonPublic | BindingFlags.Instance);
			object[] parameters = new object[] { isFil };

			SceneView[] sceneViews = (SceneView[])Resources.FindObjectsOfTypeAll(typeof(SceneView));


			foreach (SceneView sv in sceneViews)
			{
				setSearchType.Invoke(sv, parameters);


			}






		}

#endif


	}





}
