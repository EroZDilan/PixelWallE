document.addEventListener('DOMContentLoaded', function() {
    const editor = document.getElementById('code-editor');
    const lineNumbers = document.getElementById('line-numbers');
    
    // Función para actualizar los números de línea
    function updateLineNumbers() {
        const lines = editor.value.split('\n');
        lineNumbers.innerHTML = lines.map((_, i) => i + 1).join('<br>');
    }
    
    // Inicializar números de línea
    updateLineNumbers();
    
    // Actualizar números de línea cuando cambia el contenido
    editor.addEventListener('input', updateLineNumbers);
    
    // Manejar el tabulador
    editor.addEventListener('keydown', function(e) {
        if (e.key === 'Tab') {
            e.preventDefault();
            const start = this.selectionStart;
            const end = this.selectionEnd;
            
            this.value = this.value.substring(0, start) + '    ' + this.value.substring(end);
            this.selectionStart = this.selectionEnd = start + 4;
            updateLineNumbers();
        }
    });
    
    // Establecer foco en el editor al cargar
    editor.focus();
});

// Función para ejecutar el código
function executeCode() {
    const code = document.getElementById('code-editor').value;
    const canvasSize = parseInt(document.getElementById('canvas-size').value);
    const consoleOutput = document.getElementById('console');
    
    // Limpiar la consola
    consoleOutput.innerHTML = 'Ejecutando...';
    
    // Obtener el token antiforgery - VERSIÓN CORREGIDA
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    
    if (!tokenElement) {
        consoleOutput.innerHTML = 'Error: No se pudo encontrar el token de verificación.';
        console.error("No se encontró el token antiforgery en el formulario");
        return;
    }
    
    const token = tokenElement.value;
    
    // Datos a enviar
    const postData = {
        code: code,
        canvasSize: canvasSize
    };
    
    console.log("Enviando solicitud con token:", token);
    console.log("Datos:", postData);
    
    // Enviar el código al servidor
    fetch('/Index?handler=Execute', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify(postData)
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`Error HTTP: ${response.status} - ${response.statusText}`);
        }
        return response.json();
    })
    .then(data => {
        if (data.success) {
            // Actualizar el canvas con los datos de píxeles
            updateCanvas(data.pixelData, canvasSize);
            consoleOutput.innerHTML = data.consoleOutput || 'Ejecución completada con éxito.';
        } else {
            consoleOutput.innerHTML = 'Error: ' + data.error;
        }
    })
    .catch(error => {
        consoleOutput.innerHTML = 'Error: ' + error.message;
        console.error("Error en la solicitud:", error);
    });
}
// Funciones para cargar y guardar archivos
function loadFile() {
    document.getElementById('file-input').click();
}

document.getElementById('file-input').addEventListener('change', function(e) {
    const file = e.target.files[0];
    if (!file) return;
    
    const reader = new FileReader();
    reader.onload = function(e) {
        document.getElementById('code-editor').value = e.target.result;
        // Actualizar los números de línea
        const lineNumbers = document.getElementById('line-numbers');
        const lines = e.target.result.split('\n');
        lineNumbers.innerHTML = lines.map((_, i) => i + 1).join('<br>');
        
        // Confirmar carga
        const consoleOutput = document.getElementById('console');
        consoleOutput.innerHTML += `\n<span style="color: #8bc34a;">Archivo cargado: ${file.name}</span>\n`;
        consoleOutput.scrollTop = consoleOutput.scrollHeight;
    };
    reader.readAsText(file);
});

function saveFile() {
    const code = document.getElementById('code-editor').value;
    const blob = new Blob([code], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    
    const a = document.createElement('a');
    a.href = url;
    a.download = 'program.pw';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    
    // Confirmar guardado
    const consoleOutput = document.getElementById('console');
    consoleOutput.innerHTML += `\n<span style="color: #8bc34a;">Archivo guardado como program.pw</span>\n`;
    consoleOutput.scrollTop = consoleOutput.scrollHeight;
}

// Función para insertar color en el editor
function insertColorToEditor(color) {
    const editor = document.getElementById('code-editor');
    const selection = editor.selectionStart;
    
    const colorCommand = `Color("${color}")`;
    
    editor.value = editor.value.substring(0, selection) + colorCommand + editor.value.substring(editor.selectionEnd);
    editor.selectionStart = editor.selectionEnd = selection + colorCommand.length;
    
    // Actualizar números de línea
    const lineNumbers = document.getElementById('line-numbers');
    const lines = editor.value.split('\n');
    lineNumbers.innerHTML = lines.map((_, i) => i + 1).join('<br>');
    
    // Enfocar el editor
    editor.focus();
}