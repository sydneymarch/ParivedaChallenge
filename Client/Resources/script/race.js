window.onload = function () {
  loadRaceInfo();

  document.getElementById("aidForm").addEventListener("submit", handleAddAidStation);

  document.getElementById("cancelEditBtn").addEventListener("click", function () {
    document.getElementById("editOverlay").style.display = "none";
  });

  document.getElementById("cancelArrival").addEventListener("click", function () {
  document.getElementById("arrivalOverlay").style.display = "none";
  document.getElementById("arrivalTimeInput").value = "";
  });

  document.getElementById("exportExcelBtn").addEventListener("click", function () {
    const email = localStorage.getItem("email");
    const raceName = localStorage.getItem("raceName");

    const safeRunnerName = localStorage.getItem("name").replace(" ", "_");
    const safeRaceName = raceName.replace(" ", "_");
    const fileName = safeRunnerName + "-" + safeRaceName + ".xlsx";

    const index = localStorage.getItem("raceIndex");
    fetch(`/api/race/${encodeURIComponent(email)}/${index}/export`, {
      method: "GET"
    })
      .then(function (response) {
        if (!response.ok) {
          throw new Error("Export failed.");
        }
        return response.blob();
      })
      .then(function (blob) {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
      })
      .catch(function (error) {
        console.error("Error exporting Excel:", error);
        alert("Could not export to Excel.");
      });
  });

  document.getElementById("deleteStationBtn").addEventListener("click", async function () {
    const index = parseInt(document.getElementById("editOverlay").getAttribute("data-edit-index"));
    const email = localStorage.getItem("email");

    if (!confirm("Are you sure you want to delete this aid station?")) return;

    try {
      const response = await fetch(`/api/race/${email}/aidstations/${index}`, {
        method: "DELETE"
      });

      if (!response.ok) {
        alert("Could not delete aid station.");
        return;
      }

      document.getElementById("editOverlay").style.display = "none";
      loadRaceInfo();
    } catch (error) {
      console.error("Error deleting station:", error);
      alert("Something went wrong while deleting.");
    }
  });

  document.getElementById("editAidForm").addEventListener("submit", async function (event) {
    event.preventDefault();

    const index = parseInt(document.getElementById("editOverlay").getAttribute("data-edit-index"));
    const name = document.getElementById("editName").value.trim();
    const miles = parseFloat(document.getElementById("editMiles").value.trim());
    const paceInput = document.getElementById("editPace").value.trim();

    let pace = 0;
    if (paceInput.includes(":")) {
      const parts = paceInput.split(":");
      pace = parseInt(parts[0]) + (parseInt(parts[1]) / 60);
    } else {
      pace = parseFloat(paceInput);
    }

    if (!name || isNaN(miles) || isNaN(pace)) {
      alert("Please make sure all fields are filled in correctly.");
      return;
    }

    const email = localStorage.getItem("email");
    const updatedStation = {
      name: name,
      milesFromLast: miles,
      predictedPace: pace
    };

    try {
      const response = await fetch(`/api/race/${email}/aidstations/${index}/info`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(updatedStation)
      });

      if (!response.ok) {
        alert("Could not update station.");
        return;
      }

      document.getElementById("editOverlay").style.display = "none";
      loadRaceInfo();
    } catch (error) {
      console.error("Error updating station:", error);
      alert("Something went wrong.");
    }
  });

  document.getElementById("arrivalForm").addEventListener("submit", async function (event) {
    event.preventDefault();

    const email = localStorage.getItem("email");
    const index = parseInt(document.getElementById("arrivalOverlay").getAttribute("data-index"));

    const food = document.getElementById("logFood").value.trim() || null;
    const drink = document.getElementById("logDrink").value.trim() || null;
    const notes = document.getElementById("logNotes").value.trim() || null;
    const delayShift = parseInt(document.getElementById("delayShift").value) || 0;
    const rawPace = document.getElementById("paceAdjustment").value.trim();
    const paceAdjustment = rawPace === "" ? 0 : parseFloat(rawPace);

    let arrivalTime = document.getElementById("arrivalTimeInput").value;
    if (!arrivalTime) {
      arrivalTime = new Date().toLocaleString("sv-SE");
    } else {
      arrivalTime = new Date(arrivalTime).toISOString();
    }

    const updateData = {
      log: {
        food: food,
        drink: drink,
        notes: notes,
        arrivalTime: arrivalTime
      },
      delayShiftMinutes: delayShift,
      paceAdjustment: paceAdjustment
    };

    try {
      console.log("📤 Sending arrival update:", updateData);
      const response = await fetch(`/api/race/${email}/aidstations/${index}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(updateData)
      });

      if (!response.ok) {
        alert("Could not log arrival.");
        return;
      }

      document.getElementById("logFood").value = "";
      document.getElementById("logDrink").value = "";
      document.getElementById("logNotes").value = "";
      document.getElementById("delayShift").value = "";
      document.getElementById("paceAdjustment").value = "";
      document.getElementById("arrivalOverlay").style.display = "none";

      loadRaceInfo();
    } catch (error) {
      console.error("Error logging arrival:", error);
      alert("Something went wrong while logging arrival.");
    }
  });
};

function displayRaceInfo(race) {
  const startTime = new Date(race.startTime);
  document.getElementById("runnerName").textContent = race.runnerName;
  document.getElementById("raceName").textContent = race.raceName;
  document.getElementById("startTime").textContent = startTime.toLocaleTimeString([], {
    hour: "numeric",
    minute: "2-digit"
  });
  document.getElementById("startTimeRaw").value = race.startTime; // <== keep raw value for logic
  document.getElementById("totalDistance").textContent = race.totalDistance;
  document.getElementById("projectedPace").textContent = race.projectedPace.toFixed(2);
}

async function loadRaceInfo() {
  const email = localStorage.getItem("email");
  const raceName = localStorage.getItem("raceName");

  if (!email || !raceName) {
    alert("Missing race information.");
    window.location.href = "index.html";
    return;
  }

  try {
    const response = await fetch(`/api/race/${email}`);
    if (!response.ok) {
      alert("Could not load race.");
      return;
    }

    const allRaces = await response.json();
    let race = null;

    for (let i = 0; i < allRaces.length; i++) {
      if (allRaces[i].raceName === raceName) {
        race = allRaces[i];

        localStorage.setItem("raceIndex", i);
        break;
      }
    }

    if (!race) {
      alert("Race not found.");
      return;
    }

    displayRaceInfo(race);
    displayAidStations(race.aidStations);
  } catch (error) {
    console.error("Error loading race info:", error);
    alert("Something went wrong.");
  }
}

function displayAidStations(aidStations) {
  const tableBody = document.querySelector("#aidTable tbody");
  tableBody.innerHTML = "";

  const startTime = new Date(document.getElementById("startTimeRaw").value);

  let paceAdjustmentTotal = 0;

  for (let i = 0; i < aidStations.length; i++) {
    const station = aidStations[i];

    // Do not apply the current station's adjustment to itself
    const displayPace = station.predictedPace + paceAdjustmentTotal;

    const eta = new Date(station.estimatedArrival);
    const hasArrival = station.log && station.log.arrivalTime !== "0001-01-01T00:00:00";
    const logButtonLabel = hasArrival ? "Edit Arrival" : "Log Arrival";

    const row = document.createElement("tr");
    row.innerHTML = `
      <td>
        ${station.name}<br/>
        <a href="#" class="edit-link" data-index="${i}">Edit</a>
      </td>
      <td>${station.milesFromLast}</td>
      <td>${displayPace.toFixed(2)}</td>
      <td>${formatTime(eta.toLocaleString("sv-SE"))}</td>
      <td>${hasArrival ? formatTime(station.log.arrivalTime) : "—"}</td>
      <td>${station.log?.food ?? "—"}</td>
      <td>${station.log?.drink ?? "—"}</td>
      <td>${station.log?.notes ?? "—"}</td>
      <td><button class="log-btn" data-index="${i}">${logButtonLabel}</button></td>
    `;

    tableBody.appendChild(row);

    // Only start applying adjustment to future stations
    paceAdjustmentTotal += station.paceAdjustment || 0;
    setupFilters(aidStations);
  }

  function setupFilters(aidStations) {
    const searchInput = document.getElementById("searchInput");
    const unloggedCheckbox = document.getElementById("unloggedOnly");
  
    function applyFilters() {
      const search = searchInput.value.toLowerCase();
      const onlyUnlogged = unloggedCheckbox.checked;
  
      const tableBody = document.querySelector("#aidTable tbody");
      const rows = tableBody.querySelectorAll("tr");
  
      for (let i = 0; i < aidStations.length; i++) {
        const station = aidStations[i];
        const row = rows[i];
        const nameMatch = station.name.toLowerCase().includes(search);
        const isLogged = station.log?.arrivalTime !== "0001-01-01T00:00:00";
  
        const show = nameMatch && (!onlyUnlogged || !isLogged);
        row.style.display = show ? "" : "none";
      }
    }
  
    searchInput.addEventListener("input", applyFilters);
    unloggedCheckbox.addEventListener("change", applyFilters);
  }  

  // Set up "Edit" link handlers
  const editLinks = document.querySelectorAll(".edit-link");
  editLinks.forEach(link => {
    link.addEventListener("click", function (event) {
      event.preventDefault();
      const index = parseInt(this.getAttribute("data-index"));
      const station = aidStations[index];

      document.getElementById("editOverlay").style.display = "block";
      document.getElementById("editName").value = station.name;
      document.getElementById("editMiles").value = station.milesFromLast;
      document.getElementById("editPace").value = station.predictedPace;
      document.getElementById("editOverlay").setAttribute("data-edit-index", index);
    });
  });

  // Set up "Log/Edit Arrival" button handlers
  const logButtons = document.querySelectorAll(".log-btn");
  logButtons.forEach(button => {
    button.addEventListener("click", function () {
      const index = parseInt(this.getAttribute("data-index"));
      const station = aidStations[index];
      const log = station.log || {};

      document.getElementById("arrivalOverlay").style.display = "block";
      document.getElementById("arrivalOverlay").setAttribute("data-index", index);

      document.getElementById("logFood").value = log.food || "";
      document.getElementById("logDrink").value = log.drink || "";
      document.getElementById("logNotes").value = log.notes || "";

      if (log.arrivalTime && log.arrivalTime !== "0001-01-01T00:00:00") {
        const dt = new Date(log.arrivalTime);
        document.getElementById("arrivalTimeInput").value =
          dt.getFullYear() + "-" +
          String(dt.getMonth() + 1).padStart(2, "0") + "-" +
          String(dt.getDate()).padStart(2, "0") + "T" +
          String(dt.getHours()).padStart(2, "0") + ":" +
          String(dt.getMinutes()).padStart(2, "0");
      }

      document.getElementById("delayShift").value = station.delayShiftMinutes || "";
      document.getElementById("paceAdjustment").value = station.paceAdjustment || "";
    });
  });
}


function formatTime(timeString) {
  const date = new Date(timeString); // parse UTC
  if (date.getFullYear() === 1) return "—";

  // This ensures local timezone display
  return date.toLocaleTimeString(undefined, {
    hour: "numeric",
    minute: "2-digit",
    hour12: true
  });
}
async function handleAddAidStation(event) {
  event.preventDefault();

  const aidNameInput = document.getElementById("aidName");
  const milesInput = document.getElementById("milesFromLast");
  const paceInput = document.getElementById("predictedPace");

  let name = aidNameInput.value.trim();
  const milesFromLast = parseFloat(milesInput.value);
  const paceString = paceInput.value.trim();

  let predictedPace = 0;
  if (paceString.includes(":")) {
    const parts = paceString.split(":");
    const minutes = parseInt(parts[0]);
    const seconds = parseInt(parts[1]);
    predictedPace = minutes + (seconds / 60);
  } else {
    predictedPace = parseFloat(paceString);
  }

  if (!name) {
    name = "Aid " + (document.querySelectorAll("#aidTable tbody tr").length + 1);
  }


  const startTime = new Date(document.getElementById("startTimeRaw").value);

  const etaMinutes = milesFromLast * predictedPace;
  const eta = new Date(startTime.getTime() + etaMinutes * 60000);

  const station = {
    name: name,
    milesFromLast: milesFromLast,
    predictedPace: predictedPace,
    estimatedArrival: eta.toISOString(),
    log: {
      food: null,
      drink: null,
      notes: null,
      arrivalTime: "0001-01-01T00:00:00"
    }
  };

  const email = localStorage.getItem("email");

  try {
    const response = await fetch(`/api/race/${email}/aidstations`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(station)
    });

    if (!response.ok) {
      alert("Could not add aid station.");
      return;
    }

    aidNameInput.value = "";
    milesInput.value = "";
    paceInput.value = "";

    loadRaceInfo();
  } catch (error) {
    console.error("Error adding aid station:", error);
    alert("Something went wrong.");
  }
}