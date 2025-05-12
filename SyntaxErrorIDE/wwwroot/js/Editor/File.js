document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('save-btn').addEventListener('click', saveFile);
    document.getElementById('create-btn').addEventListener('click', createNewFile);

    loadFile('App_Data/CodeFiles/test.txt');
});

function saveFile() {
    const codeEditor = document.getElementById('code-editor');
    const filename = document.getElementById('file-explorer').textContent;
    if (filename && codeEditor.value !== "") {
        const blob = new Blob([codeEditor.value], { type: 'text/plain' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        link.click();
    }
}

function createNewFile() {
    const newFilename = document.getElementById('new-filename');
    if (newFilename && newFilename.value !== "") {
        const fileExplorer = document.getElementById('file-explorer');
        fileExplorer.textContent = newFilename.value;
        newFilename.textContent = "";
        fileExplorer.onclick = function() {
            loadFile(newFilename.value);
        };
        fileExplorer.appendChild(fileExplorer);
    }
}

function loadFile(filename) {
    const codeEditor = document.getElementById('code-editor');
    codeEditor.value = `aaaa`;
}