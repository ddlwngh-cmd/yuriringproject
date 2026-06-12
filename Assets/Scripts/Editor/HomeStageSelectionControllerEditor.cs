using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HomeStageSelectionController))]
public class HomeStageSelectionControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HomeStageSelectionController controller = (HomeStageSelectionController)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("코인 치트", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("저장된 총 코인", CoinStorage.LoadTotalCoin().ToString());

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("코인 0으로 초기화"))
            {
                controller.ResetTotalCoin();
            }

            if (GUILayout.Button("코인 100 추가"))
            {
                controller.AddCheatCoin();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("강화 치트", EditorStyles.boldLabel);
        if (GUILayout.Button("모든 강화 레벨 0으로 초기화"))
        {
            controller.ResetAllUpgradeLevels();
        }
    }
}
