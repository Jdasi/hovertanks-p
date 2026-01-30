using System.IO;
using UnityEngine;

namespace HoverTanks.IO
{
    public class ProfileData
    {
        public string Username;
    }

    public static class ProfileIO
    {
        private const string FILE_NAME = "Profile.ini";
        private const string NAME_KEY = "Name";

        public static ProfileData GetProfileData()
        {
            var path = GetPath();

            if (!File.Exists(path))
            {
                SaveProfileData("");
            }

            var reader = new StreamReader(path);
            var data = new ProfileData();
            int lineNumber = 0;

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] split = line.Split('=');

                ++lineNumber;

                if (split == null
                    || split.Length < 1
                    || split.Length > 2)
                {
                    Log.Info(LogChannel.ProfileIO, $"GetProfileData - bad line number: {lineNumber}");
                    continue;
                }

                ReadToData(split[0], split.Length == 2 ? split[1] : "", data);
            }

            reader.Close();
            return data;
        }

        private static void ReadToData(string key, string val, ProfileData data)
        {
            switch (key)
            {
                case NAME_KEY: data.Username = val; break;
            }
        }

        public static void SaveProfileData(string username)
        {
            var path = GetPath();
            var writer = new StreamWriter(path);

            Log.Info(LogChannel.ProfileIO, $"SaveProfileData - {path}");

            writer.WriteLine($"{NAME_KEY}={username}");

            writer.Close();
        }

        private static string GetPath()
        {
            var dir = Application.persistentDataPath;
            return $"{dir}\\{FILE_NAME}";
        }
    }
}
