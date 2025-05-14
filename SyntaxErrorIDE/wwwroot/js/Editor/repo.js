document.getElementById("load-repo-btn").addEventListener("click", async () => {
    const url = document.getElementById("github-url").value.trim();
    if (!url) {
        updateStatus("Voer een GitHub URL in");
        return;
    }

    updateStatus("Repository laden...");

    try {
        const response = await fetch("/Editor/DownloadRepo", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ githubUrl: url })
        });

        if (response.ok) {
            const files = await response.json();
            showFilesInExplorer(files);
            updateStatus(`Repository geladen: ${files.length} bestanden`);
        } else {
            const error = await response.text();
            updateStatus(`Fout: ${error}`, true);
        }
    } catch (error) {
        updateStatus(`Netwerkfout: ${error.message}`, true);
    }
});

function showFilesInExplorer(files) {
    const explorer = document.getElementById("file-explorer");
    explorer.innerHTML = "";

    files.forEach(file => {
        const div = document.createElement("div");
        div.className = "file-item";
        div.textContent = file;
        div.onclick = async () => {
            try {
                updateStatus(`Bestand laden: ${file}`);
                const res = await fetch(`/Editor/GetFileContent?file=${encodeURIComponent(file)}`);
                if (res.ok) {
                    document.getElementById("code-editor").value = await res.text();
                    updateLineNumbers();
                    updateStatus(`Geladen: ${file}`);
                } else {
                    updateStatus(`Fout bij laden bestand: ${file}`, true);
                }
            } catch (error) {
                updateStatus(`Fout: ${error.message}`, true);
            }
        };
        explorer.appendChild(div);
    });
}

function updateStatus(message, isError = false) {
    const statusBar = document.getElementById("status-bar");
    statusBar.textContent = message;
    statusBar.style.color = isError ? "#dc3545" : "#495057";
}