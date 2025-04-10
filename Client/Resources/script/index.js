async function handleOnLoad() {
    const raceApiUrl = "http://localhost:5251/api/race";

    try {
        console.log("made it")
        const response = await fetch(raceApiUrl);
        const runners = await response.json();
        console.log(runners);

        let html = "";

        if (runners.length === 0) {
            html = "<p>No runners found.</p>";
        } else {
            runners.forEach(runner => {
                html += `<div class="runner-card">
                    <p><b>Runner:</b> ${runner.name}</p>
                    <p><b>Email:</b> ${runner.email}</p>
                    <p><b>Phone:</b> ${runner.phone}</p>
                </div>`;
            });
        }

        document.getElementById("runners").innerHTML = html;
    } catch (error) {
        console.error("Error fetching runners:", error);
        document.getElementById("runners").innerHTML = "<p>Failed to load runners</p>";
    }
}
console.onload = handleOnLoad;
