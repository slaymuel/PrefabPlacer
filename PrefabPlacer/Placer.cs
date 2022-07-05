using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace PrefabPlacer
{
    public class Placer : EditorWindow
    {
        //Preprocessor directive, if symbol UNITY_EDITOR is defined

        private bool enabled = false;
        //Clamp randomized size in XZ
        private bool clampAspectXZ = false;
        //Clamp randomized size in XY
        private bool clampAspectXY = false;
        //Automatically rotate along normal of ground
        private bool autoRotate = false;
        private GameObject prefab;
        private GameObject placedObject;
        private Transform parent;
        //Which layer should we raycast against when placing objects
        private LayerMask layerMask;
        private float randomScaleMinX = 1f;
        private float randomScaleMinY = 1f;
        private float randomScaleMinZ = 1f;
        private float randomScaleMaxX = 1f;
        private float randomScaleMaxY = 1f;
        private float randomScaleMaxZ = 1f;
        private float minRotation = 0f;
        private float maxRotation = 0f;
        private float maxAspectXZ = 1f;
        private float minAspectXZ = 1f;
        private float maxAspectXY = 1f;
        private float minAspectXY = 1f;
        private float scrollSensitivity = 1f;
        private int layerBackup;
        private float normalRotationDegree;

        //Called when opening
        [MenuItem ("Tools/Prefab Placer")]
        static void Init()
        {
            Placer window = (Placer)EditorWindow.GetWindow(typeof(Placer));
            window.Show();
        }

        private void OnEnable()
        {
            //Subscribe to duringSceneGui event, called when sceneview calls OnGUI
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
            //EditorGUILayout.Toggle("Enable ", placePrefabs);
            enabled = UnityEditor.EditorGUILayout.Toggle("Enable", enabled);
            autoRotate = EditorGUILayout.Toggle("Auto rotate", autoRotate);
            if (autoRotate)
                normalRotationDegree = EditorGUILayout.Slider("Rotation strength",normalRotationDegree, 0f, 1f);
            else
                normalRotationDegree = 0f;

            prefab = (GameObject)EditorGUILayout.ObjectField("Prefab:", prefab, typeof(GameObject), true);
            parent = (Transform)EditorGUILayout.ObjectField("Parent to spawned object:", parent, typeof(Transform), true);

            LayerMask tempMask = EditorGUILayout.MaskField("Layer", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask), InternalEditorUtility.layers);
            if (tempMask.value == 0)
            {
                Debug.LogWarning("Prefab Placer: You have to select a layer to place object on! Setting layer to Default.");
                tempMask = 1;
            }
            layerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);

            clampAspectXY = EditorGUILayout.Toggle("Clamp aspect ratio in XY", clampAspectXY);
            clampAspectXZ = EditorGUILayout.Toggle("Clamp aspect ratio in XZ", clampAspectXZ);
        
            EditorGUILayout.LabelField("SCALING");

            EditorGUILayout.MinMaxSlider("Random scale X", ref randomScaleMinX, ref randomScaleMaxX, 0f, 10f);
            EditorGUILayout.BeginHorizontal();
            randomScaleMinX = EditorGUILayout.FloatField(randomScaleMinX);
            randomScaleMaxX = EditorGUILayout.FloatField(randomScaleMaxX);
            EditorGUILayout.EndHorizontal();

            if (!clampAspectXY)
            {
                EditorGUILayout.MinMaxSlider("Random scale Y", ref randomScaleMinY, ref randomScaleMaxY, 0f, 10f);
                EditorGUILayout.BeginHorizontal();
                randomScaleMinY = EditorGUILayout.FloatField(randomScaleMinY);
                randomScaleMaxY = EditorGUILayout.FloatField(randomScaleMaxY);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.MinMaxSlider("Aspect clamp X / Y", ref minAspectXY, ref maxAspectXY, 0f, 10f);
                EditorGUILayout.BeginHorizontal();
                minAspectXY = EditorGUILayout.FloatField(minAspectXY);
                maxAspectXY = EditorGUILayout.FloatField(maxAspectXY);
                EditorGUILayout.EndHorizontal();
            }

            if (!clampAspectXZ)
            {
                EditorGUILayout.MinMaxSlider("Random scale Z", ref randomScaleMinZ, ref randomScaleMaxZ, 0f, 10f);
                EditorGUILayout.BeginHorizontal();
                randomScaleMinZ = EditorGUILayout.FloatField(randomScaleMinZ);
                randomScaleMaxZ = EditorGUILayout.FloatField(randomScaleMaxZ);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.MinMaxSlider("Aspect clamp X / Z", ref minAspectXZ, ref maxAspectXZ, 0f, 10f);
                EditorGUILayout.BeginHorizontal();
                minAspectXZ = EditorGUILayout.FloatField(minAspectXZ);
                maxAspectXZ = EditorGUILayout.FloatField(maxAspectXZ);
                EditorGUILayout.EndHorizontal();
            }


            EditorGUILayout.LabelField("ROTATION");

            EditorGUILayout.MinMaxSlider("Random rotation", ref minRotation, ref maxRotation, 0f, 360f);
            EditorGUILayout.BeginHorizontal();
            minRotation = EditorGUILayout.FloatField(minRotation);
            maxRotation = EditorGUILayout.FloatField(maxRotation);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Input Settings");
            scrollSensitivity = EditorGUILayout.Slider("Scroll sens", scrollSensitivity, 0f, 10f);
        }

        private void OnSceneGUI(SceneView view)
        {
            //Do not try to capture any mouse events if not enabled by user
            if (!enabled)
                return;

            //Get the current event
            Event e = Event.current;

            if (e.type == EventType.ScrollWheel && placedObject && e.button == 0)
            {
                OnMouseScroll(e);
                //Use the event / Swallow the input
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && placedObject && e.button == 0)
            {
                OnMouseDrag(e);
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 0)
            {
                OnMouseClick(e);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && placedObject && e.button == 0)
            {
                Utility.MapOntoAll(placedObject, delegate (GameObject obj) { obj.gameObject.layer = layerBackup; });
                placedObject = null;
                e.Use();
            }
            else if (e.type == EventType.Layout)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
        }

        private void OnMouseScroll(Event e)
        {
            placedObject.transform.Rotate(new Vector3(0f, scrollSensitivity * e.delta[1], 0f));
        }
        private void OnMouseClick(Event e)
        {
            if (RaycastFromMouse(e, out RaycastHit hit))
            {
                placedObject = (GameObject)PrefabUtility.InstantiatePrefab(PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefab));
                layerBackup = placedObject.layer;
                //Change layer so that raycast doesnt hit placed object
                Utility.MapOntoAll(placedObject, delegate (GameObject obj) { obj.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); });

                placedObject.transform.position = hit.point;
                placedObject.transform.localScale = Utility.RandomVector(new Vector2(randomScaleMinX, randomScaleMaxX),
                                                                         new Vector2(randomScaleMinY, randomScaleMaxY),
                                                                         new Vector2(randomScaleMinZ, randomScaleMaxZ),
                                                                         clampAspectXZ ? new Vector2(minAspectXZ, maxAspectXZ) : new Vector2(0f, 0f),
                                                                         clampAspectXY ? new Vector2(minAspectXY, maxAspectXY) : new Vector2(0f, 0f));

                if (autoRotate)
                    placedObject.transform.rotation = Quaternion.FromToRotation(placedObject.transform.up, 
                                                                               Vector3.Lerp(this.prefab.transform.up, hit.normal, normalRotationDegree)) * 
                                                                               placedObject.transform.rotation;

                //placedObject.transform.rotation = Quaternion.FromToRotation(placedObject.transform.up, hit.normal) * placedObject.transform.rotation;

                placedObject.transform.RotateAround(hit.point, placedObject.transform.up, Random.Range(minRotation, maxRotation));

                placedObject.transform.SetParent(parent);
                Undo.RegisterCreatedObjectUndo(placedObject, "spawn object");
            }
            else
                Debug.LogWarning("Prefab Placer: Raycast did not hit anything, are you raycasting against the correct layer?");
        }

        private void OnMouseDrag(Event e)
        {
            if (RaycastFromMouse(e, out RaycastHit hit))
            {
                if (autoRotate)
                    placedObject.transform.rotation = Quaternion.FromToRotation(placedObject.transform.up,
                                                                               Vector3.Lerp(this.prefab.transform.up, hit.normal, normalRotationDegree)) *
                                                                               placedObject.transform.rotation;

                placedObject.transform.position = hit.point;
            }
        }

        private bool RaycastFromMouse(Event e, out RaycastHit hit)
        {
            if (Physics.Raycast(Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, Camera.current.pixelHeight - e.mousePosition.y, 0)), out hit,
                            Mathf.Infinity, layerMask))
            {
                return true;
            }
            return false;
        }

    }
}