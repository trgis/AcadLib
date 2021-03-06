namespace AcadLib.Doc
{
    using System.Collections.Generic;
    using System.Linq;
    using AutoCAD_PIK_Manager.Settings;
    using Autodesk.AutoCAD.DatabaseServices;
    using JetBrains.Annotations;
    using NetLib;

    public static class DbDictionaryCleaner
    {
        private static List<List<string>> cleanPaths;

        public static void Start()
        {
            cleanPaths = PikSettings.PikFileSettings.DbDictionaryCleans?.Select(GetPathKeys)?.ToList();
        }

        /// <summary>
        /// Не вызывать в момент открытия чертежа - приводит к фаталам!!!
        /// </summary>
        public static void Clean([NotNull] Database db)
        {
            if (cleanPaths == null || cleanPaths.Count == 0) return;
            cleanPaths.ForEach(p => Clean(db.NamedObjectsDictionaryId, p));
        }

        private static void Clean(ObjectId dictId, List<string> keys)
        {
            if (keys.Count == 0) return;
            foreach (var key in keys)
            {
#pragma warning disable 618
                using var dict = dictId.Open(OpenMode.ForRead, false, true) as DBDictionary;
#pragma warning restore 618
                if (dict?.Contains(key) == true)
                {
                    dictId = dict.GetAt(key);
                }
                else
                {
                    Logger.Log.Warn($"DbDictionaryCleaner Не найден словарь по пути: {keys.JoinToString("/")}");
                    return;
                }
            }

#pragma warning disable 618
            using var dbo = dictId.Open(OpenMode.ForWrite, false, true);
#pragma warning restore 618
            dbo?.Erase();

            var msg = $"DbDictionaryCleaner: Удален словарь по пути '{keys.JoinToString("/")}'";
            msg.WriteToCommandLine();
            Logger.Log.Info(msg);
        }

        private static List<string> GetPathKeys(string path)
        {
            return path.Split('\\', '/').Where(w => !w.IsNullOrEmpty()).ToList();
        }
    }
}
