document.getElementById("registerForm").addEventListener("submit", async function (event) {
    event.preventDefault();
  
    const name = document.getElementById("name").value.trim();
    const email = document.getElementById("email").value.trim();
    const password = document.getElementById("password").value.trim();
    const role = document.getElementById("role").value.trim();
  
    if (!name || !email || !password || !role) {
      alert("Please fill in all fields.");
      return;
    }
  
    const userData = {
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
  
      if (response.ok) {
        alert("Registration successful! Redirecting to login...");
        window.location.href = "login.html";
      } else if (response.status === 409) {
        alert("That email is already registered.");
      } else {
        alert("Something went wrong during registration.");
      }
    } catch (error) {
      console.error("Error during registration:", error);
      alert("Could not register. Check your network or try again later.");
    }
  });
  