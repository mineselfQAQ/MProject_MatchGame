using UnityEditor;
using UnityEngine;

namespace MFramework
{
    [CustomEditor(typeof(MCore))]
    public class MCoreEditor : Editor
    {
        public static Texture2D LOGOTex;

        //***ע��***��SerializedProperty��Ҫ[SerializeField]���ܻ�ȡ
        private SerializedProperty logStateSP;
        private SerializedProperty UICustomLoadStateSP;
        private SerializedProperty localStateSP;
        private SerializedProperty performanceStateSP;
        private SerializedProperty fpsDisplayModeSP;
        private SerializedProperty fpsSampleDurationSP;
        private SerializedProperty fpsKeycodeSP;

        private MCore mCore;

        /// <summary>
        /// ����MCoreʱ�ᴥ��һ��(���Hierarchy�µ�MCore��Inspector�Ͽ���MCore)
        /// </summary>
        private void OnEnable()
        {
            LOGOTex = MTextureLibrary.LOGOTex;
            
            mCore = (MCore)target;
            
            logStateSP = serializedObject.FindProperty("m_LogState");
            UICustomLoadStateSP = serializedObject.FindProperty("m_UICustomLoadState");
            localStateSP = serializedObject.FindProperty("m_LocalState");
            performanceStateSP = serializedObject.FindProperty("m_PerformanceState");
            fpsDisplayModeSP = serializedObject.FindProperty("m_FPSDisplayMode");
            fpsSampleDurationSP = serializedObject.FindProperty("m_FPSSampleDuration");
            fpsKeycodeSP = serializedObject.FindProperty("m_keycode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            MEditorGUIUtility.DrawTexture(LOGOTex, MEditorGUIStyleUtility.CenterStyle);

            MEditorGUIUtility.DrawH2("�༭��");
            MEditorControlUtility.DrawBoolPopup(UICustomLoadStateSP, "�Ƿ�����UI�Զ������");
            MEditorGUIUtility.DrawH2("���");
            MEditorControlUtility.DrawBoolPopup(logStateSP, "�Ƿ����LOG��Ϣ");
            MEditorGUIUtility.DrawH2("���ػ�");
            MEditorControlUtility.DrawBoolPopup(localStateSP, "�Ƿ������ػ�");
            MEditorGUIUtility.DrawH2("���ܼ��");
            bool flag = MEditorControlUtility.DrawBoolPopup(performanceStateSP, "�Ƿ������ܼ��");
            if (flag)
            {
                EditorGUI.indentLevel++;
                MEditorControlUtility.DrawPopup<FPSMonitor.DisplayMode>(fpsDisplayModeSP, "FPS��ʾģʽ");
                MEditorControlUtility.DrawFloat(fpsSampleDurationSP, "�������(��)");
                MEditorControlUtility.DrawPopup<PerformanceMonitor.PKeycode>(fpsKeycodeSP, "��ʾ/���ذ���");
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
