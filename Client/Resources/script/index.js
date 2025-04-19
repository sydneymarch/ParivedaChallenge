window.onload = function () {
  loadDashboard();
};

async function loadDashboard() {
  const email = localStorage.getItem("email");
  const name = localStorage.getItem("name");

  if (!email || !name) {
    window.location.href = "login.html";
    return;
  }

  document.getElementById("userName").textContent = name;

  document.getElementById("logoutBtn").addEventListener("click", logoutUser);
  document.getElementById("addRaceBtn").addEventListener("click", function () {
    document.getElementById("addRaceOverlay").style.display = "flex";
  });
  document.getElementById("closeAddRace").addEventListener("click", function () {
    document.getElementById("addRaceOverlay").style.display = "none";
  });

  document.getElementById("addRaceForm").addEventListener("submit", async function (e) {
    e.preventDefault();

    const raceNameInput = document.getElementById("raceName");
    const startTimeInput = document.getElementById("startTime");

    const raceName = raceNameInput.value;
    const startTime = startTimeInput.value;
    const runnerName = localStorage.getItem("name");
    const email = localStorage.getItem("email");

    const newRace = {
      raceName: raceName,
      startTime: startTime,
      runnerName: runnerName,
      email: email,
      aidStations: [],
      sharedWithEmails: []
    };

    try {
      const response = await fetch("/api/race", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(newRace)
      });

      if (response.status === 409) {
        alert("This race already exists.");
        return;
      }

      if (!response.ok) {
        alert("Could not create race.");
        return;
      }

      document.getElementById("addRaceOverlay").style.display = "none";
      raceNameInput.value = "";
      startTimeInput.value = "";
      refreshRaceList(email);
    } catch (err) {
      console.error("Error creating race:", err);
      alert("Something went wrong.");
    }
  });

  refreshRaceList(email);
}

async function refreshRaceList(email) {
  try {
    const response = await fetch("/api/race/" + email);
    if (!response.ok) return;

    const raceList = await response.json();
    const raceListElement = document.getElementById("raceList");
    raceListElement.innerHTML = "";

    const seen = new Set();

    for (let i = 0; i < raceList.length; i++) {
      const race = raceList[i];
      if (seen.has(race.raceName)) continue;
      seen.add(race.raceName);

      const listItem = document.createElement("li");
      listItem.textContent = race.raceName;
      listItem.className = "race-link";

      listItem.addEventListener("click", function () {
        localStorage.setItem("raceName", race.raceName);
        window.location.href = "race.html";
      });

      raceListElement.appendChild(listItem);
    }
  } catch (err) {
    console.error("Error loading races:", err);
    alert("Could not load races.");
  }
}

function logoutUser() {
  localStorage.clear();
  window.location.href = "login.html";
}
