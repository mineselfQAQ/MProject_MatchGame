using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace MFramework
{
    public enum AtlasGeneratorMode
    {
        File,//��һ�ļ����
        Directory,//���ļ��д��
        All//��������ڲ��ļ���
    }

    public class SpriteAtlasGenerator : EditorWindow
    {
        private Object inputObj;
        private Object outputFolder;

        private bool isFirstOpen = true;//�״δ�
        private Vector2 settingPos;

        //===����===
        //�ҵ�����
        private string prefix = "Atlas";
        private AtlasGeneratorMode mode = AtlasGeneratorMode.Directory;
        //��������
        //private bool _includeInBuild = true;//�������ɣ�������ҪLateBinding
        //�������
        private int _blockOffset = 0;
        private int _padding = 2;
        private bool _allowRotation = false;
        private bool _tightPacking = false;
        private bool _alphaDilation = false;
        //��ͼ����
        private bool _readable = false;
        private bool _generateMipMaps = false;
        private bool _sRGB = true;
        private FilterMode _filterMode = FilterMode.Bilinear;
        //ƽ̨����
        private int _maxTextureSize = 2048;
        private TextureImporterFormat _textureImporterFormat = TextureImporterFormat.Automatic;
        private TextureImporterCompression _textureImporterCompression = TextureImporterCompression.Compressed;
        private bool _crunchedCompression = true;
        private int _compressionQuality = 50;

        [MenuItem("MFramework/Sprite Atlas Generator", false, 910)]
        public static void Init()
        {
            var window = GetWindow<SpriteAtlasGenerator>(true, "SpriteAtlasGenerator");
            window.minSize = new Vector2(400, 560);
            window.maxSize = new Vector2(400, 560);
            window.Show();
        }

        private void OnGUI()
        {
            MEditorGUIUtility.DrawH1("ͼ���������");
            {
                DrawInputOutputSettings();

                EditorGUILayout.Space(20);

                MEditorGUIUtility.DrawLeftH2("����");
                {
                    settingPos = EditorGUILayout.BeginScrollView(settingPos);
                    {
                        DrawMainSettings();
                    }
                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.Space(10);

                DrawGenerateBtn();
            }
        }

        private void DrawInputOutputSettings()
        {
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("������ݣ�");

                GUILayout.FlexibleSpace();

                //TODO����չΪList
                if (isFirstOpen)
                {
                    isFirstOpen = false;

                    Object obj = GetSelectedProjectObject();
                    obj = CheckFolderOrSprite(obj);
                    if (obj != null)
                    {
                        inputObj = obj;
                    }
                }
                inputObj = EditorGUILayout.ObjectField(inputObj, typeof(object), false, GUILayout.Width(110));
                inputObj = CheckFolderOrSprite(inputObj);

                mode = (AtlasGeneratorMode)EditorGUILayout.EnumPopup(mode, GUILayout.Width(75));
            });

            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("����ļ��У�");

                outputFolder = EditorGUILayout.ObjectField(outputFolder, typeof(object), false);
                outputFolder = CheckFolder(outputFolder);
            });
        }

        private void DrawMainSettings()
        {
            //��������
            MEditorGUIUtility.DrawLeftH3("��������");
            DrawBaseSettings();
            //�������
            MEditorGUIUtility.DrawLeftH3("�������");
            DrawPackingSettings();
            //��ͼ����
            MEditorGUIUtility.DrawLeftH3("��ͼ����");
            DrawTextureSettings();
            //ƽ̨����
            MEditorGUIUtility.DrawLeftH3("ƽ̨����");
            DrawPlatformSettings();
        }
        private void DrawBaseSettings()
        {

            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("ǰ׺����", GUILayout.Width(50));
                prefix = EditorGUILayout.TextField(prefix);
                MEditorGUIUtility.DrawText($"����{prefix}_XXX");
            });
        }
        private void DrawPackingSettings()
        {
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("����BlockOffset��", "ָ����һ��Sprite����Χ��Ӽ��");
                GUILayout.FlexibleSpace();
                _blockOffset = EditorGUILayout.IntField(_blockOffset, GUILayout.Width(30));
            });
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("������Padding��", "ָ����ÿ��Sprite����Χ��Ӽ��");
                GUILayout.FlexibleSpace();
                _padding = EditorGUILayout.IntField(_padding, GUILayout.Width(30));
            });
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("�Ƿ�������תAllowRotation��");
                GUILayout.FlexibleSpace();
                _allowRotation = EditorGUILayout.Toggle(_allowRotation, GUILayout.Width(15));
            });
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("�Ƿ���մ��TightPacking��");
                GUILayout.FlexibleSpace();
                _tightPacking = EditorGUILayout.Toggle(_tightPacking, GUILayout.Width(15));
            });
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("���ñ߽�AlphaΪ0(AlphaDilation)��");
                GUILayout.FlexibleSpace();
                _alphaDilation = EditorGUILayout.Toggle(_alphaDilation, GUILayout.Width(15));
            });
        }
        private void DrawTextureSettings()
        {
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("�Ƿ�ɶ�Readable��");
                GUILayout.FlexibleSpace();
                _readable = EditorGUILayout.Toggle(_readable, GUILayout.Width(15));
            });
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("�Ƿ�����Mipmaps��");
                GUILayout.FlexibleSpace();
                _generateMipMaps = EditorGUILayout.Toggle(_generateMipMaps, GUILayout.Width(15));
            });
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("�Ƿ�ʹ��sRGB��");
                GUILayout.FlexibleSpace();
                _sRGB = EditorGUILayout.Toggle(_sRGB, GUILayout.Width(15));
            });
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("����ģʽFilterMode��");
                GUILayout.FlexibleSpace();
                _filterMode = (FilterMode)EditorGUILayout.EnumPopup(_filterMode, GUILayout.Width(100));
            });
        }
        private void DrawPlatformSettings()
        {
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("�����ͼ��СMaxTextureSize��");
                GUILayout.FlexibleSpace();
                _maxTextureSize = EditorGUILayout.IntField(_maxTextureSize, GUILayout.Width(45));
            });
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("��ʽFormat��");
                GUILayout.FlexibleSpace();
                _textureImporterFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup(_textureImporterFormat, GUILayout.Width(100));
            });
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("ѹ����ʽCompression��");
                GUILayout.FlexibleSpace();
                _textureImporterCompression = (TextureImporterCompression)EditorGUILayout.EnumPopup(_textureImporterCompression, GUILayout.Width(100));
            });
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("�Ƿ�������ѹ��CrunchedCompression��");
                GUILayout.FlexibleSpace();
                _crunchedCompression = EditorGUILayout.Toggle(_crunchedCompression, GUILayout.Width(15));
            });
            MEditorGUIUtility.Horizontal(() =>
            {
                MEditorGUIUtility.DrawText("����ѹ������CompressionQuality��");
                GUILayout.FlexibleSpace();
                _compressionQuality = EditorGUILayout.IntField(_compressionQuality, GUILayout.Width(30));
            });
        }

        private void DrawGenerateBtn()
        {
            if (GUILayout.Button("����"))
            {
                if (inputObj == null || outputFolder == null)
                {
                    MLog.Print($"{typeof(SpriteAtlasGenerator)}�����������ļ����ٽ�������", MLogType.Warning);
                }

                switch (mode)
                {
                    case AtlasGeneratorMode.File:
                    {
                        if (!IsSprite(inputObj))
                        {
                            MLog.Print($"{typeof(SpriteAtlasGenerator)}��Fileģʽ�봫��Sprite", MLogType.Warning);
                            return;
                        }

                        string name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(inputObj));
                        CreateAtlas(name, inputObj);

                        break;
                    }
                    case AtlasGeneratorMode.Directory:
                    {
                        if (!IsFolder(inputObj))
                        {
                            MLog.Print($"{typeof(SpriteAtlasGenerator)}��Directoryģʽ�봫���ļ���", MLogType.Warning);
                            return;
                        }

                        string name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(inputObj));
                        CreateAtlas(name, inputObj);

                        break;
                    }
                    case AtlasGeneratorMode.All:
                    {
                        if (!IsFolder(inputObj))
                        {
                            MLog.Print($"{typeof(SpriteAtlasGenerator)}��Allģʽ�봫���ļ���", MLogType.Warning);
                            return;
                        }

                        //Allģʽ�ռ��߼���
                        //�ռ�����ļ���
                        //TODO�����Կ����ռ�����Sprite
                        List<string> deepestFolders = new List<string>();
                        string path = AssetDatabase.GetAssetPath(inputObj);
                        GetSubFolders(path, deepestFolders);

                        Object[] objs = new Object[deepestFolders.Count];
                        int n = 0;
                        foreach (var folderPath in deepestFolders)
                        {
                            var folder = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
                            objs[n++] = folder;

                            string name = Path.GetFileNameWithoutExtension(folderPath);
                            CreateAtlas(name, folder);
                        }
                        //TODO��������һ��Atlas����Ӷ���ļ��У�����Ŀǰ�Ķ��Atlas

                        break;
                    }
                }

                MLog.Print($"{typeof(SpriteAtlasGenerator)}��������");
            }
        }

        private void GetSubFolders(string path, List<string> deepestFolders)
        {
            string[] subPaths = AssetDatabase.GetSubFolders(path);

            if (subPaths.Length == 0)
            {
                //˵��Ϊ�����ļ��У������ռ�
                string[] assets = AssetDatabase.FindAssets("", new[] { path });
                if (assets.Length != 0)//����Դ�����
                {
                    deepestFolders.Add(path);
                }
            }
            else
            {
                foreach (var subPath in subPaths)
                {
                    GetSubFolders(subPath, deepestFolders);
                }
            }
        }

        private void CreateAtlas(string name, params Object[] objs)
        {
            SpriteAtlas atlas = new SpriteAtlas();
            atlas.Add(objs);
            SetAtlasInfo(ref atlas);
            string path = $"{AssetDatabase.GetAssetPath(outputFolder)}/Atlas_{name}.spriteatlas";
            AssetDatabase.CreateAsset(atlas, path);
        }

        private void SetAtlasInfo(ref SpriteAtlas atlas)
        {
            SpriteAtlasPackingSettings packSetting = new SpriteAtlasPackingSettings()
            {
                blockOffset = _blockOffset,
                enableRotation = _allowRotation,
                enableTightPacking = _tightPacking,
                enableAlphaDilation = _alphaDilation,
                padding = _padding,
            };
            SpriteAtlasTextureSettings textureSetting = new SpriteAtlasTextureSettings()
            {
                readable = _readable,
                generateMipMaps = _generateMipMaps,
                sRGB = _sRGB,
                filterMode = _filterMode,
            };
            TextureImporterPlatformSettings platformSetting = new TextureImporterPlatformSettings()
            {

                maxTextureSize = _maxTextureSize,
                format = _textureImporterFormat,
                crunchedCompression = _crunchedCompression,
                textureCompression = _textureImporterCompression,
                compressionQuality = _compressionQuality,
            };

            atlas.SetIncludeInBuild(true);
            atlas.SetPackingSettings(packSetting);
            atlas.SetTextureSettings(textureSetting);
            atlas.SetPlatformSettings(platformSetting);
        }

        private bool IsTexture2D(Object obj)
        {
            return obj is Texture2D;
        }
        private bool IsSprite(Object obj)
        {
            return obj is Sprite;
        }
        private bool IsFolder(Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(path))
            {
                return true;
            }
            return false;
        }

        private Object CheckFolderOrSprite(Object obj)
        {
            if (obj == null) return null;

            if (IsFolder(obj) || IsSprite(obj))
            {
                return obj;
            }
            else if (IsTexture2D(obj))
            {
                Sprite sprite = GetSprite((Texture2D)obj);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            MLog.Print($"{typeof(SpriteAtlasGenerator)}��ֻ֧���ļ���/���飬������ѡ��", MLogType.Warning);
            return null;
        }
        private Object CheckFolder(Object obj)
        {
            if (obj == null) return null;

            if (IsFolder(obj))
            {
                return obj;
            }

            MLog.Print($"{typeof(SpriteAtlasGenerator)}��ֻ֧���ļ��У�������ѡ��", MLogType.Warning);
            return null;
        }

        private Sprite GetSprite(Texture2D texture)
        {
            if (IsTexture2D(texture))
            {
                string path = AssetDatabase.GetAssetPath(texture);
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                //����Texture2D��Sprite��Ӧ��ϵ�������
                //Tip������Spriteģʽ�����Ի�ȡ��Texture2D��Դ��Sprite��Դ
                if (subAssets.Length == 2)
                {
                    if (subAssets[1] is Sprite sprite) return sprite;
                }
            }
            return null;
        }

        private Object GetSelectedProjectObject()
        {
            Object[] objs = MSelection.projectObjects;
            if (objs.Length != 1) return null;

            return objs[0];
        }
    }
}
