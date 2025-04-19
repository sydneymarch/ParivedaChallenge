window.onload = function () {
    const loginForm = document.getElementById("loginForm");
    const registerLink = document.getElementById("registerLink");
  
    loginForm.addEventListener("submit", handleLogin);
    registerLink.addEventListener("click", function (event) {
      event.preventDefault();
      window.location.href = "register.html";
    });
  };
  
  async function handleLogin(event) {
    event.preventDefault();
  
    const emailInput = document.getElementById("email");
    const passwordInput = document.getElementById("password");
  
    const email = emailInput.value;
    const password = passwordInput.value;
  
    const loginInfo = {
      email: email,
      password: password
    };
  
    try {
      const loginResponse = await fetch("/api/race/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(loginInfo)
      });
  
      if (!loginResponse.ok) {
        alert("Login failed. Please check your email and password.");
        return;
      }
  
      const user = await loginResponse.json();
      localStorage.setItem("email", user.email);
      localStorage.setItem("name", user.name);
      localStorage.setItem("role", user.role);
  
      const raceResponse = await fetch(`http://localhost:5251/api/race/${user.email}`);
  
      if (raceResponse.ok) {
        const raceList = await raceResponse.json();
  
        if (raceList.length > 0) {
          window.location.href = "index.html";
        } else {
          window.location.href = "createRace.html";
        }
      } else if (raceResponse.status === 404) {
        window.location.href = "createRace.html";
      } else {
        alert("Something went wrong while checking races.");
      }
  
    } catch (error) {
      console.error("Login error:", error);
      alert("Something went wrong while logging in.");
    }
  }
  
  function handleRegister() {
    alert("Registration isn't ready yet!");
  }
  