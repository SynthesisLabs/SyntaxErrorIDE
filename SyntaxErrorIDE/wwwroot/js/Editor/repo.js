document.addEventListener('DOMContentLoaded', () => {
    const editor = document.getElementById('code-editor');
    const lineNumbers = document.getElementById('line-numbers');
    const fileExplorer = document.getElementById('file-explorer');
    const loadRepoBtn = document.getElementById('load-repo-btn');
    const saveFileBtn = document.getElementById('save-file-btn');
    const githubUrlInput = document.getElementById('github-url');
    const statusBar = document.getElementById('status-bar');
    const breadcrumb = document.getElementById('breadcrumb');

    let currentRepo = {
        owner: null,
        repo: null,
        path: "",
        currentFile: null,
        fileSha: null
    };

    function updateStatus(message, isError = false) {
        statusBar.textContent = message;
        statusBar.className = `mt-2 p-2 text-sm min-h-8 rounded ${isError ? 'text-red-600 bg-red-50' : 'text-gray-600 bg-gray-100'}`;

        if (!isError) {
            setTimeout(() => {
                if (statusBar.textContent === message) {
                    statusBar.textContent = '';
                }
            }, 5000);
        }
    }

    function updateLineNumbers() {
        const lines = editor.value.split('\n').length;
        let numbers = '';
        for (let i = 1; i <= lines; i++) {
            numbers += i + '\n';
        }
        lineNumbers.innerText = numbers;
    }

    function syncScroll() {
        lineNumbers.scrollTop = editor.scrollTop;
    }

    function updateBreadcrumb(path) {
        if (!path) {
            breadcrumb.innerHTML = '<span class="text-gray-500">Geen bestand geselecteerd</span>';
            return;
        }

        const segments = path.split('/');
        breadcrumb.innerHTML = '';

        const repoSpan = document.createElement('span');
        repoSpan.className = 'text-blue-600 hover:underline cursor-pointer mr-1';
        repoSpan.textContent = `${currentRepo.owner}/${currentRepo.repo}`;
        repoSpan.addEventListener('click', () => {
            loadDirectoryContents('');
        });
        breadcrumb.appendChild(repoSpan);

        if (segments.length > 0 && segments[0] !== '') {
            const separator = document.createElement('span');
            separator.textContent = ' / ';
            separator.className = 'text-gray-500 mx-1';
            breadcrumb.appendChild(separator);
        }

        let currentPath = '';
        segments.forEach((segment, index) => {
            if (segment === '') return;

            currentPath += (currentPath ? '/' : '') + segment;

            const segmentSpan = document.createElement('span');

            if (index < segments.length - 1) {
                segmentSpan.className = 'text-blue-600 hover:underline cursor-pointer';
                segmentSpan.textContent = segment;

                const pathToLoad = currentPath;
                segmentSpan.addEventListener('click', () => {
                    loadDirectoryContents(pathToLoad);
                });
            } else {
                segmentSpan.className = 'font-medium';
                segmentSpan.textContent = segment;
            }

            breadcrumb.appendChild(segmentSpan);

            if (index < segments.length - 1) {
                const separator = document.createElement('span');
                separator.textContent = ' / ';
                separator.className = 'text-gray-500 mx-1';
                breadcrumb.appendChild(separator);
            }
        });
    }

    editor.addEventListener('input', () => {
        updateLineNumbers();
        if (currentRepo.currentFile) saveFileBtn.disabled = false;
    });

    editor.addEventListener('scroll', syncScroll);

    loadRepoBtn.addEventListener('click', loadRepo);
    saveFileBtn.addEventListener('click', saveFile);

    githubUrlInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') loadRepo();
    });

    async function loadRepo() {
        const url = githubUrlInput.value.trim();
        if (!url) {
            updateStatus('Voer een GitHub repo URL in (gebruiker/repo of volledige URL)', true);
            return;
        }

        const repoInfo = parseGitHubUrl(url);
        if (!repoInfo) {
            updateStatus('Ongeldige GitHub URL. Gebruik formaat: gebruiker/repo of https://github.com/gebruiker/repo', true);
            return;
        }

        currentRepo.owner = repoInfo.owner;
        currentRepo.repo = repoInfo.repo;
        currentRepo.path = "";
        currentRepo.currentFile = null;
        currentRepo.fileSha = null;

        updateStatus(`Repository laden: ${repoInfo.owner}/${repoInfo.repo}...`);
        saveFileBtn.disabled = true;
        editor.value = '';
        updateLineNumbers();
        updateBreadcrumb('');

        try {
            await loadDirectoryContents('');
        } catch (error) {
            updateStatus(`Fout bij laden repository: ${error.message}`, true);
        }
    }

    async function loadDirectoryContents(path) {
        updateStatus(`Map laden: ${path || 'hoofdmap'}...`);
        currentRepo.path = path;
        currentRepo.currentFile = null;
        currentRepo.fileSha = null;

        try {
            const response = await fetch(
                `/api/GitHub/repo/${currentRepo.owner}/${currentRepo.repo}/contents/${path}`
            );

            if (!response.ok) {
                const error = await response.text();
                throw new Error(`GitHub API error: ${response.status} - ${error}`);
            }

            const contents = await response.json();

            editor.value = '';
            updateLineNumbers();
            saveFileBtn.disabled = true;
            updateBreadcrumb(path);

            showFilesInExplorer(contents, path);
        } catch (error) {
            updateStatus(`Fout bij laden map: ${error.message}`, true);
        }
    }

    function showFilesInExplorer(contents, path) {
        fileExplorer.innerHTML = "";

        if (!Array.isArray(contents)) {
            updateStatus("Ongeldig antwoord van GitHub API", true);
            return;
        }

        contents.sort((a, b) => {
            if (a.type !== b.type) {
                return a.type === 'dir' ? -1 : 1;
            }
            return a.name.localeCompare(b.name);
        });

        if (path && path !== '') {
            const upDir = document.createElement("div");
            upDir.className = "file-item p-1 hover:bg-gray-200 cursor-pointer flex items-center";

            const iconSpan = document.createElement("span");
            iconSpan.textContent = '📁';
            iconSpan.className = "mr-1";
            upDir.appendChild(iconSpan);

            const textSpan = document.createElement("span");
            textSpan.textContent = "..";
            textSpan.className = "font-medium";
            upDir.appendChild(textSpan);

            upDir.onclick = () => {
                const parentPath = path.includes('/')
                    ? path.substring(0, path.lastIndexOf('/'))
                    : '';
                loadDirectoryContents(parentPath);
            };
            fileExplorer.appendChild(upDir);
        }

        contents.forEach(item => {
            const div = document.createElement("div");
            div.className = "file-item p-1 hover:bg-gray-200 cursor-pointer flex items-center";

            const iconSpan = document.createElement("span");
            if (item.type === 'dir') iconSpan.textContent = '📁';
            else {
                const extension = item.name.split('.').pop().toLowerCase();
                let icon = '📄';
                
                // Programming languages
                if (['js', 'ts', 'jsx', 'tsx', 'mjs', 'cjs'].includes(extension)) icon = '📜';
                if (['html', 'htm', 'xhtml', 'cshtml'].includes(extension)) icon = '🌐';
                if (['css', 'scss', 'sass', 'less', 'styl'].includes(extension)) icon = '🎨';
                if (['json', 'xml', 'yaml', 'yml', 'toml', 'fxml'].includes(extension)) icon = '📋';
                if (['md', 'markdown', 'rst'].includes(extension)) icon = '📝';
                if (['cs', 'fs', 'vb'].includes(extension)) icon = '🔷';
                if (['java', 'class', 'jar', 'jsp'].includes(extension)) icon = '☕';
                if (['kt', 'kts'].includes(extension)) icon = '☕';
                if (['py', 'pyc', 'pyo', 'pyd', 'whl'].includes(extension)) icon = '🐍';
                if (['rb', 'erb', 'gemspec'].includes(extension)) icon = '💎';
                if (['php', 'phtml', 'php3', 'php4', 'php5', 'phps'].includes(extension)) icon = '🐘';
                if (['cpp', 'cxx', 'cc', 'hpp', 'hxx', 'hh'].includes(extension)) icon = '🔵';
                if (['c', 'h'].includes(extension)) icon = '🔴';
                if (['go', 'mod', 'sum'].includes(extension)) icon = '🐹';
                if (['rs', 'rlib'].includes(extension)) icon = '🦀';
                if (['swift'].includes(extension)) icon = '🐦';
                if (['scala', 'sc'].includes(extension)) icon = '🌀';
                if (['sh', 'bash', 'zsh', 'fish'].includes(extension)) icon = '💻';
                if (['bat', 'cmd'].includes(extension)) icon = '🪟';
                if (['ps1', 'psm1', 'psd1'].includes(extension)) icon = '🔋';
                if (['lua'].includes(extension)) icon = '🌙';
                if (['pl', 'pm'].includes(extension)) icon = '🐪';
                if (['r', 'R'].includes(extension)) icon = '📊';
                if (['dart'].includes(extension)) icon = '🎯';
                if (['elm'].includes(extension)) icon = '🌳';
                if (['clj', 'cljs', 'cljc', 'edn'].includes(extension)) icon = '☘️';
                if (['hs', 'lhs'].includes(extension)) icon = 'λ';
                if (['ex', 'exs'].includes(extension)) icon = '💜';
                if (['sql', 'psql', 'pgsql', 'mysql'].includes(extension)) icon = '🗃️';
                if (['asm', 's'].includes(extension)) icon = '🖥️';

                // Data files
                if (['csv', 'tsv', 'xls', 'xlsx', 'ods'].includes(extension)) icon = '📊';
                if (['db', 'sqlite', 'sqlite3', 'mdb'].includes(extension)) icon = '🗄️';

                // Media files
                if (['png', 'jpg', 'jpeg', 'gif', 'svg', 'webp', 'bmp'].includes(extension)) icon = '🖼️';
                if (['mp3', 'wav', 'flac', 'aac', 'ogg'].includes(extension)) icon = '🎵';
                if (['mp4', 'avi', 'mov', 'mkv', 'webm', 'flv'].includes(extension)) icon = '🎬';

                // Documents
                if (['pdf'].includes(extension)) icon = '📕';
                if (['doc', 'docx', 'odt'].includes(extension)) icon = '📘';
                if (['ppt', 'pptx', 'odp'].includes(extension)) icon = '📙';
                if (['txt', 'log'].includes(extension)) icon = '📃';

                // Archives
                if (['zip', 'rar', '7z', 'tar', 'gz', 'bz2', 'xz'].includes(extension)) icon = '🗜️';

                // System files
                if (['exe', 'dll', 'so', 'dylib', 'a', 'ini',
                    'conf', 'cfg', 'properties', 'env', 'dotenv'].includes(extension)) icon = '⚙️';
                
                iconSpan.textContent = icon;
            }
            iconSpan.className = "mr-1";
            div.appendChild(iconSpan);

            const textSpan = document.createElement("span");
            textSpan.textContent = item.name;
            textSpan.className = "truncate";
            div.appendChild(textSpan);

            div.onclick = async () => {
                try {
                    if (item.type === 'dir') {
                        const newPath = path ? `${path}/${item.name}` : item.name;
                        loadDirectoryContents(newPath);
                    } else await loadFile(item, path);
                } catch (error) {
                    updateStatus(`Fout: ${error.message}`, true);
                }
            };
            fileExplorer.appendChild(div);
        });

        updateStatus(`${contents.length} items gevonden in: ${path || 'hoofdmap'}`);
    }

    async function loadFile(fileInfo, currentPath) {
        try {
            saveFileBtn.disabled = true;
            updateStatus(`Bestand laden: ${fileInfo.name}...`);

            const filePath = currentPath ? `${currentPath}/${fileInfo.name}` : fileInfo.name;

            const url = `https://api.github.com/repos/${currentRepo.owner}/${currentRepo.repo}/contents/${filePath}`;

            const response = await fetch(url);

            if (!response.ok) throw new Error(`Fout bij laden bestand: ${response.status}`);

            const fileData = await response.json();

            if (!fileData.content) throw new Error("Bestandsinhoud niet gevonden");

            const decodedContent = atob(fileData.content.replace(/\n/g, ''));
            editor.value = decodedContent;
            updateLineNumbers();

            currentRepo.currentFile = filePath;
            currentRepo.fileSha = fileData.sha;

            saveFileBtn.disabled = false;
            updateBreadcrumb(filePath);
            updateStatus(`Bestand geladen: ${filePath}`);

            document.querySelectorAll('.file-item').forEach(item => {
                item.classList.remove('bg-blue-100');
                if (item.querySelector('span:last-child').textContent === fileInfo.name)
                    item.classList.add('bg-blue-100');
            });
        } catch (error) {
            updateStatus(`Fout bij laden bestand: ${error.message}`, true);
        }
    }

    async function saveFile() {
        if (!currentRepo.currentFile || !currentRepo.fileSha) {
            updateStatus('Geen bestand geselecteerd om op te slaan', true);
            return;
        }

        try {
            updateStatus(`Bestand opslaan: ${currentRepo.currentFile}...`);

            const response = await fetch(
                `/api/GitHub/save/${currentRepo.owner}/${currentRepo.repo}/${currentRepo.currentFile}`,
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({
                        content: editor.value,
                        sha: currentRepo.fileSha,
                        message: `Update ${currentRepo.currentFile} via Syntax Error IDE`
                    })
                }
            );

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`GitHub API error (${response.status}): ${errorText}`);
            }

            const responseData = await response.json();
            if (responseData.content && responseData.content.sha) {
                currentRepo.fileSha = responseData.content.sha;
            }

            updateStatus(`Bestand opgeslagen: ${currentRepo.currentFile}`);
            saveFileBtn.disabled = true;
        } catch (error) {
            updateStatus(`Fout bij opslaan: ${error.message}`, true);
        }
    }

    function parseGitHubUrl(input) {
        if (input.match(/^[a-zA-Z0-9-_.]+\/[a-zA-Z0-9-_.]+$/)) {
            const [owner, repo] = input.split('/');
            return {owner, repo};
        }

        try {
            const url = new URL(input);
            if (!url.hostname.includes('github.com'))
                return null;

            const pathParts = url.pathname.split('/').filter(Boolean);
            if (pathParts.length >= 2) {
                return {
                    owner: pathParts[0],
                    repo: pathParts[1].replace('.git', '')
                };
            }
        } catch (e) {

        }

        return null;
    }

    editor.addEventListener('keydown', function (e) {
        if (e.key === 'Tab') {
            e.preventDefault();
            const start = this.selectionStart;
            const end = this.selectionEnd;

            this.value = this.value.substring(0, start) + '    ' + this.value.substring(end);
            this.selectionStart = this.selectionEnd = start + 4;
        }
    });

    updateLineNumbers();
    updateStatus('Klaar om te beginnen. Laad een repository om te starten.');
});