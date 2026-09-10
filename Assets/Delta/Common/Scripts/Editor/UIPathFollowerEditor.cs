using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIPathFollower))]
public class UIPathFollowerEditor : Editor
{
    private UIPathFollower follower;
    private bool isPreviewing = false;
    private double lastEditorTime;

    void OnEnable()
    {
        follower = (UIPathFollower)target;
    }

    void OnDisable()
    {
        StopPreview();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var animTypeProp = serializedObject.FindProperty("animationType");
        bool isFixedDuration = animTypeProp.enumValueIndex == (int)UIPathFollower.AnimationType.LoopFixedDuration;
        if (!isFixedDuration)
            EditorGUILayout.HelpBox("fixedDuration is only used by LoopFixedDuration.", MessageType.None);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        if (Application.isPlaying)
        {
            // Runtime controls
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play"))   follower.Play();
            if (GUILayout.Button("Pause"))  follower.Pause();
            if (GUILayout.Button("Resume")) follower.Resume();
            if (GUILayout.Button("Stop"))   follower.Stop();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Is Playing", follower.IsPlaying.ToString());
        }
        else
        {
            // Edit-mode preview
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !isPreviewing;
            if (GUILayout.Button("▶  Play Preview"))
            {
                follower.Play();
                StartPreview();
            }
            GUI.enabled = isPreviewing;
            if (GUILayout.Button("■  Stop Preview"))
                StopPreview();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (isPreviewing)
                EditorGUILayout.HelpBox("Previewing in edit mode…", MessageType.Info);
        }
    }

    void StartPreview()
    {
        if (isPreviewing) return;
        isPreviewing = true;
        lastEditorTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += EditorTick;
    }

    void StopPreview()
    {
        if (!isPreviewing) return;
        isPreviewing = false;
        EditorApplication.update -= EditorTick;
        follower.Stop();
        SceneView.RepaintAll();
    }

    void EditorTick()
    {
        if (follower == null) { StopPreview(); return; }

        double now = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(now - lastEditorTime);
        lastEditorTime = now;

        follower.Tick(deltaTime);

        // Mark dirty so the scene reflects the position change
        EditorUtility.SetDirty(follower);
        SceneView.RepaintAll();

        // Stop automatically when a OneTime animation finishes
        if (!follower.IsPlaying)
            StopPreview();
    }

    // ── Scene handles ────────────────────────────────────────────────────────

    void OnSceneGUI()
    {
        if (follower.waypoints == null || follower.waypoints.Count == 0) return;

        Transform parent = follower.transform.parent;

        Undo.RecordObject(follower, "Move UIPathFollower Waypoint");

        for (int i = 0; i < follower.waypoints.Count; i++)
        {
            Vector3 worldPos = LocalToWorld(parent, follower.waypoints[i]);

            EditorGUI.BeginChangeCheck();
            Vector3 newWorld = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                follower.waypoints[i] = WorldToLocal(parent, newWorld);
                EditorUtility.SetDirty(follower);
            }

            Handles.Label(worldPos + Vector3.up * 20f, i.ToString(), EditorStyles.boldLabel);
        }

        if (follower.curveType == UIPathFollower.CurveType.Bezier
            && follower.bezierHandles != null
            && follower.bezierHandles.Count == follower.waypoints.Count)
        {
            Handles.color = Color.magenta;

            for (int i = 0; i < follower.waypoints.Count; i++)
            {
                Vector3 wpWorld   = LocalToWorld(parent, follower.waypoints[i]);
                Vector3 hOutWorld = LocalToWorld(parent, follower.waypoints[i] + follower.bezierHandles[i]);
                Vector3 hInWorld  = LocalToWorld(parent, follower.waypoints[i] - follower.bezierHandles[i]);

                Handles.DrawLine(wpWorld, hOutWorld);
                Handles.DrawLine(wpWorld, hInWorld);

                EditorGUI.BeginChangeCheck();
                Vector3 newOut = Handles.FreeMoveHandle(hOutWorld, 10f, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    follower.bezierHandles[i] = WorldToLocal(parent, newOut) - follower.waypoints[i];
                    EditorUtility.SetDirty(follower);
                }

                EditorGUI.BeginChangeCheck();
                Vector3 newIn = Handles.FreeMoveHandle(hInWorld, 10f, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    follower.bezierHandles[i] = follower.waypoints[i] - WorldToLocal(parent, newIn);
                    EditorUtility.SetDirty(follower);
                }
            }
        }
    }

    static Vector3 LocalToWorld(Transform parent, Vector2 localPos)
    {
        if (parent != null)
            return parent.TransformPoint(new Vector3(localPos.x, localPos.y, 0f));
        return new Vector3(localPos.x, localPos.y, 0f);
    }

    static Vector2 WorldToLocal(Transform parent, Vector3 worldPos)
    {
        if (parent != null)
        {
            Vector3 local = parent.InverseTransformPoint(worldPos);
            return new Vector2(local.x, local.y);
        }
        return new Vector2(worldPos.x, worldPos.y);
    }
}
