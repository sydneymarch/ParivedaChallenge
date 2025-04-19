window.onload = function () {
    const form = document.getElementById("createRaceForm");
    form.addEventListener("submit", handleCreateRace);
  };
  
  async function handleCreateRace(event) {
    event.preventDefault(); // Stop the form from submitting normally
  
    const raceNameInput = document.getElementById("raceName");
    const startTimeInput = document.getElementById("startTime");
  
    const raceName = raceNameInput.value;
    const startTime = startTimeInput.value;
  
    const email = localStorage.getItem("email");
    const name = localStorage.getItem("name");
  
    const newRace = {
      runnerName: name,
      email: email,
      raceName: raceName,
      startTime: startTime,
      aidStations: [],
      sharedWithEmails: []
    };
  
    try {
      const response = await fetch("/api/race", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(newRace)
      });
  
      if (!response.ok) {
        alert("Could not create race.");
        return;
      }
  
      alert("Race created!");
      window.location.href = "index.html";
    } catch (error) {
      console.error("Error creating race:", error);
      alert("Something went wrong.");
    }
  }
  