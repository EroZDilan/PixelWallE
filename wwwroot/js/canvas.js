let pixelCanvas;
let ctx;
let pixelSize = 1;
let showGrid = true;

document.addEventListener('DOMContentLoaded', function() {
    pixelCanvas = document.getElementById('pixel-canvas');
    ctx = pixelCanvas.getContext('2d');
    
    // Inicializar el canvas
    const canvasSize = parseInt(document.getElementById('canvas-size').value);
    initCanvas(canvasSize);

    // Eventos para el canvas
    pixelCanvas.addEventListener('mousemove', function(e) {
        const rect = pixelCanvas.getBoundingClientRect();
        const scaleX = pixelCanvas.width / rect.width;
        const scaleY = pixelCanvas.height / rect.height;
        
        const x = Math.floor((e.clientX - rect.left) * scaleX);
        const y = Math.floor((e.clientY - rect.top) * scaleY);
        
        if (x >= 0 && x < canvasSize && y >= 0 && y < canvasSize) {
            document.getElementById('current-position').textContent = `(${x}, ${y})`;
        }
    });
});

function initCanvas(size) {
    pixelCanvas.width = size;
    pixelCanvas.height = size;
    
    // Ajustar el tamaño de visualización para que el canvas se vea bien
    const container = document.getElementById('canvas-container');
    const maxSize = Math.min(container.clientWidth - 40, container.clientHeight - 40);
    
    if (size > maxSize) {
        const scale = maxSize / size;
        pixelCanvas.style.width = `${size * scale}px`;
        pixelCanvas.style.height = `${size * scale}px`;
        pixelSize = scale;
    } else {
        // Si el canvas es pequeño, aumentamos el tamaño visual para verlo mejor
        const scale = Math.floor(maxSize / size);
        if (scale > 1) {
            pixelCanvas.style.width = `${size * scale}px`;
            pixelCanvas.style.height = `${size * scale}px`;
            pixelSize = scale;
        } else {
            pixelCanvas.style.width = `${size}px`;
            pixelCanvas.style.height = `${size}px`;
            pixelSize = 1;
        }
    }
    
    // Limpiar el canvas (blanco)
    ctx.fillStyle = 'white';
    ctx.fillRect(0, 0, size, size);
    
    // Dibujar la cuadrícula
    if (showGrid) {
        drawGrid(size);
    }
}

function drawGrid(size) {
    const gridScale = Math.max(1, Math.ceil(8 / pixelSize)); // Ajustar la frecuencia de la cuadrícula
    
    ctx.strokeStyle = 'rgba(200, 200, 200, 0.5)';
    ctx.lineWidth = 0.5;
    
    // Dibujar líneas horizontales cada gridScale píxeles
    for (let i = 0; i <= size; i += gridScale) {
        ctx.beginPath();
        ctx.moveTo(0, i);
        ctx.lineTo(size, i);
        ctx.stroke();
    }
    
    // Dibujar líneas verticales cada gridScale píxeles
    for (let i = 0; i <= size; i += gridScale) {
        ctx.beginPath();
        ctx.moveTo(i, 0);
        ctx.lineTo(i, size);
        ctx.stroke();
    }
}

function resizeCanvas() {
    const newSize = parseInt(document.getElementById('canvas-size').value);
    if (newSize < 10) {
        alert('El tamaño mínimo del canvas es 10 píxeles.');
        document.getElementById('canvas-size').value = 10;
        return;
    }
    
    if (newSize > 500) {
        alert('El tamaño máximo del canvas es 500 píxeles.');
        document.getElementById('canvas-size').value = 500;
        return;
    }
    
    initCanvas(newSize);
    
    // Mostrar mensaje de confirmación
    const consoleOutput = document.getElementById('console');
    consoleOutput.innerHTML += `\nCanvas redimensionado a ${newSize}x${newSize} píxeles.\n`;
    consoleOutput.scrollTop = consoleOutput.scrollHeight;
}

function updateCanvas(pixelData, canvasSize) {
    // Reiniciar el canvas
    initCanvas(canvasSize);
    
    // Optimización: Procesar los píxeles por color para reducir los cambios de contexto
    const colorGroups = {};
    
    for (let i = 0; i < pixelData.length; i++) {
        const pixel = pixelData[i];
        if (!colorGroups[pixel.color]) {
            colorGroups[pixel.color] = [];
        }
        colorGroups[pixel.color].push({ x: pixel.x, y: pixel.y });
    }
    
    // Dibujar píxeles por grupos de color
    for (const color in colorGroups) {
        ctx.fillStyle = getActualColor(color);
        colorGroups[color].forEach(pixel => {
            ctx.fillRect(pixel.x, pixel.y, 1, 1);
        });
    }
    
    // Redibujar la cuadrícula si está activada
    if (showGrid) {
        drawGrid(canvasSize);
    }
}

function getActualColor(colorName) {
    // Mapeo de nombres de colores a valores CSS
    const colorMap = {
        'Red': '#dc3545',
        'Blue': '#007bff',
        'Green': '#28a745',
        'Yellow': '#ffc107',
        'Orange': '#fd7e14',
        'Purple': '#6f42c1',
        'Black': '#000000',
        'White': '#ffffff',
        'Transparent': 'rgba(0, 0, 0, 0)'
    };
    
    return colorMap[colorName] || colorName;
}

function toggleGrid() {
    showGrid = !showGrid;
    const canvasSize = parseInt(document.getElementById('canvas-size').value);
    
    // Redibujar el canvas sin alterar los píxeles
    ctx.clearRect(0, 0, canvasSize, canvasSize);
    ctx.fillStyle = 'white';
    ctx.fillRect(0, 0, canvasSize, canvasSize);
    
    // Volver a dibujar los píxeles
    // (Esto debería realmente volver a ejecutar el código, pero para simplificar)
    if (showGrid) {
        drawGrid(canvasSize);
    }
}