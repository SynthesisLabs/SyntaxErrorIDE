document.addEventListener('DOMContentLoaded', function() {
    const codeEditor = document.getElementById('code-editor');
    const lineNumbers = document.getElementById('line-numbers');

    function updateLineNumbers() {
        const lines = codeEditor.value.split('\n');
        const lineCount = lines.length;
        
        let numbersText = '';
        for (let i = 1; i <= lineCount; i++) {
            numbersText += i + '\n';
        }
        
        if (lineCount === 0) numbersText = '1';

        lineNumbers.textContent = numbersText;
        lineNumbers.scrollTop = codeEditor.scrollTop;
    }

    codeEditor.addEventListener('input', updateLineNumbers);
    codeEditor.addEventListener('scroll', () => {
        lineNumbers.scrollTop = codeEditor.scrollTop;
    });
    
    lineNumbers.textContent = '1';
});