const API_BASE_URL = 'http://your-api-endpoint';

export async function sendCommand(piId: string, command: 'restart' | 'shutdown' | 'reboot') {
  try {
    const response = await fetch(`${API_BASE_URL}/command`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        type: command,
        target: piId,
      }),
    });
    
    if (!response.ok) {
      throw new Error('Failed to send command');
    }
    
    return await response.json();
  } catch (error) {
    console.error('Error sending command:', error);
    throw error;
  }
}