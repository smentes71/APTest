function showNotification(message, type = 'success') {
    const notification = document.createElement('div');
    notification.className = `fixed bottom-4 right-4 p-4 rounded-lg shadow-lg ${
        type === 'success' ? 'bg-green-500' : 'bg-red-500'
    } text-white`;
    notification.textContent = message;
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.remove();
    }, 3000);
}

async function sendCommand(piId, command) {
    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        const response = await fetch('/Home/SendCommand', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ piId, command })
        });
        
        const data = await response.json();
        
        if (data.success) {
            showNotification('Command sent successfully');
        } else {
            showNotification(data.message || 'Failed to send command', 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        showNotification('Failed to send command', 'error');
    }
}