using api.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace api
{
    public class UserFile
    {
        private string folderPath = "users";

        // reads all .json user files from the "users" folder
        public List<User> GetAllUsers()
        {
            List<User> users = new List<User>();

            // make sure folder exists first
            if (!Directory.Exists(folderPath))
            {
                return users;
            }

            string[] files = Directory.GetFiles(folderPath, "*.json");

            for (int i = 0; i < files.Length; i++)
            {
                string json = File.ReadAllText(files[i]);
                User user = JsonSerializer.Deserialize<User>(json);

                if (user != null)
                {
                    users.Add(user);
                }
            }

            return users;
        }

        // saves a single user as a JSON file
        public void SaveUser(User user)
        {
            // make sure folder exists
            Directory.CreateDirectory(folderPath);

            // make filename safe by removing symbols
            string safeEmail = user.Email.Replace("@", "_at_").Replace(".", "_dot_");
            string filePath = Path.Combine(folderPath, safeEmail + ".json");

            string json = JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
