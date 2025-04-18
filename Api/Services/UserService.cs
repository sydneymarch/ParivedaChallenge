using api.Models;
using System.Collections.Generic;

namespace api.Services
{
    public class UserService
    {
        private UserFile userFile = new UserFile(); // handles loading/saving from users/ folder

        // checks if a user already exists by their email
        public bool UserExists(string email)
        {
            List<User> allUsers = userFile.GetAllUsers();

            for (int i = 0; i < allUsers.Count; i++)
            {
                string existingEmail = allUsers[i].Email.ToLower();
                string newEmail = email.ToLower();

                if (existingEmail == newEmail)
                {
                    return true; // user is already saved
                }
            }

            return false; // user not found
        }

        // adds a new user if the email isn't already taken
        public bool RegisterUser(User newUser)
        {
            if (UserExists(newUser.Email))
            {
                return false; // don't allow duplicates
            }

            userFile.SaveUser(newUser); // save to disk
            return true;
        }

        // logs in by checking if email + password match a saved user
        public User LoginUser(string email, string password)
        {
            List<User> allUsers = userFile.GetAllUsers();

            for (int i = 0; i < allUsers.Count; i++)
            {
                string existingEmail = allUsers[i].Email.ToLower();
                string savedPassword = allUsers[i].Password;

                if (existingEmail == email.ToLower() && savedPassword == password)
                {
                    return allUsers[i]; // login successful!
                }
            }

            return null; // login failed
        }
    }
}
