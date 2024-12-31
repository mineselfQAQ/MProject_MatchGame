using System.IO;
using UnityEngine;

namespace MFramework
{
    //TODO:����ֵ�洢����Settings�ļ�
    [MonoSingletonSetting(HideFlags.NotEditable, "#MSerializationManager#")]
    public class MSerializationManager : MonoSingleton<MSerializationManager>
    {
        public string settingsPath;
        public CoreSettings CoreSettings => CreateOrGetSettings<CoreSettings>(settingsPath);

        private void Awake()
        {
            //�������ȡCoreSettings
            settingsPath = $"{MSettings.PersistentDataPath}/CoreSettings.json";

            if (File.Exists(settingsPath))
            {
                CreateOrGetSettings<CoreSettings>(settingsPath);
            }
        }

        public T CreateOrGetSettings<T>(string path) where T : new()
        {
            if (File.Exists(path)) 
            {
                return MSerializationUtility.ReadFromJson<T>(path);
            }

            if (MSerializationUtility.SaveToJson<T>(path, new T(), true))
            {
                return MSerializationUtility.ReadFromJson<T>(path);
            }
            return default(T);
        }
    }
}
