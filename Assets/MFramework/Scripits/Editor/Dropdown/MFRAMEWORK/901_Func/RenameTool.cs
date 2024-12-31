using MFramework;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class RenameTool : EditorWindow
{
    public string _newName;
    public int _startValue = 0;

    private Vector2 namePos;

    //����---������
    //�У�
    //NewName---���º������
    //StartValue---��׺��ʼֵ
    [MenuItem("MFramework/Rename Tool", false, 902)]
    public static void Init()
    {
        RenameTool window = GetWindow<RenameTool>(true, "RenameTool");
        window.minSize = new Vector2(320, 150);
        window.maxSize = new Vector2(320, 1000);
        window.Show();
    }


    private void OnGUI()
    {
        //***����NewName��StartValue***
        _newName = EditorGUILayout.TextField("NewName:", _newName);
        _startValue = EditorGUILayout.IntField("StartValue", _startValue);

        EditorGUILayout.Space(20);

        //***��ʾ����ǰ����ĺ����ֵı仯***
        var hierarchyObjs = Selection.gameObjects;
        var projectObjs = MSelection.projectObjects;
        bool selectedHierarchy = false, selectedProject = false;
        bool hasObject = false;

        if (hierarchyObjs.Length != 0) { selectedHierarchy = true; hasObject = true; }
        if (projectObjs.Length != 0) { selectedProject = true; hasObject = true; }

        if (selectedHierarchy && selectedProject)
        {
            MLog.Print($"{typeof(RenameTool)}������ͬʱѡ��Hierarchy��Project�е�����", MLogType.Warning);
            return;
        }

        Object[] objs = null;
        if (selectedHierarchy)
        {
            //TODO������Ӧ�û�����������ĵ������������״̬����Ҫ����
            var selectObject = Selection.gameObjects.OrderBy(obj => obj.transform.GetSiblingIndex());
            objs = selectObject.ToArray();
        }
        else
        {
            objs = projectObjs;
        }

        int i = 0;
        if (hasObject)
        {
            EditorGUILayout.LabelField("���ĺ�:");

            namePos = EditorGUILayout.BeginScrollView(namePos);
            {
                foreach (var obj in objs)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{obj.name}--->{_newName}_{_startValue + i}");
                    EditorGUILayout.EndHorizontal();
                    i++;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space(20);

        //***ִ�в���***
        //ǿ����ǰ״̬��
        //��ɫ---����ִ��
        //��ɫ---����ִ�У���Ϊû��ѡ�����壬���Ե���ȥҲû��Ӧ
        GUI.enabled = hasObject;//����״̬����ɫ��ʱ���ǻҵ�---���ɵ��
        if (hasObject)
        {
            GUI.color = Color.green;
        }
        else
        {
            GUI.color = Color.red;
        }

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("Execute"))//һ��ִ��
            {
                i = 0;
                foreach (var obj in objs)
                {
                    if (selectedHierarchy)
                    {
                        obj.name = $"{_newName}_{_startValue + i}";
                        EditorUtility.SetDirty(obj);
                    }
                    else
                    {
                        string path = AssetDatabase.GetAssetPath(obj);
                        AssetDatabase.RenameAsset(path, $"{_newName}_{_startValue + i}");
                    }
                    i++;
                }
            }
            if (GUILayout.Button("Execute(NoSuffix)"))//������׺ִ��
            {
                i = 0;
                foreach (var obj in objs)
                {
                    if (selectedHierarchy)
                    {
                        obj.name = $"{_newName}";
                        EditorUtility.SetDirty(obj);
                    }
                    else
                    {
                        string path = AssetDatabase.GetAssetPath(obj);
                        AssetDatabase.RenameAsset(path, $"{_newName}");
                    }
                    i++;
                }
            }
            if (GUILayout.Button("Execute(OnlySuffix)"))//ֻ�����׺ִ�У��磺0 1 2 3
            {
                i = 0;
                foreach (var obj in objs)
                {
                    if (selectedHierarchy)
                    {
                        obj.name = $"{_startValue + i}";
                        EditorUtility.SetDirty(obj);
                    }
                    else
                    {
                        string path = AssetDatabase.GetAssetPath(obj);
                        AssetDatabase.RenameAsset(path, $"{_startValue + i}");
                    }
                    i++;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        if (selectedProject)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}