document.getElementById("registerForm").addEventListener("submit", async function (event) {
    event.preventDefault(); //prevent refresh
  
    //get values from form fields removing any accidental spaces
    const name = document.getElementById("name").value.trim();
    const email = document.getElementById("email").value.trim();
    const password = document.getElementById("password").value.trim();
    const role = document.getElementById("role").value.trim();
  
    if (!name || !email || !password || !role) {
      alert("Please fill in all fields.");
      return; //makes sure all fields are filled
    }
  
    const userData = { //create object to send to c#
      name: name,
      email: email,
      password: password,
      role: role
    };
  
    try {
      const response = await fetch("/api/user/register", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify(userData)
      });
  
      if (response.ok) { //if request worked user info saved in local storage
        alert("Registration successful! Redirecting to login...");
        window.location.href = "login.html";
      } else if (response.status === 409) { //api sends 409 if email already exists
        alert("That email is already registered.");
      } else {
        alert("Something went wrong during registration.");
      }
    } catch (error) { //catch any errors
      console.error("Error during registration:", error);
      alert("Could not register. Check your network or try again later.");
    }
  });
  