import React, { useState } from 'react';
import { PiCard } from './components/PiCard';
import { sendCommand } from './services/api';
import type { RaspberryPi } from './types/types';

function App() {
  // This would typically come from your API
  const [raspberryPis] = useState<RaspberryPi[]>([
    {
      id: '1',
      name: 'Pi Living Room',
      status: 'online',
      ipAddress: '192.168.1.100'
    },
    {
      id: '2',
      name: 'Pi Kitchen',
      status: 'offline',
      ipAddress: '192.168.1.101'
    },
    {
      id: '3',
      name: 'Pi Garage',
      status: 'online',
      ipAddress: '192.168.1.102'
    }
  ]);

  const handleCommand = async (piId: string, command: 'restart' | 'shutdown' | 'reboot') => {
    try {
      await sendCommand(piId, command);
      // Here you would typically update the UI or refresh the Pi status
    } catch (error) {
      console.error('Failed to execute command:', error);
      // Handle error (show notification, etc.)
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 p-8">
      <div className="max-w-6xl mx-auto">
        <h1 className="text-3xl font-bold text-gray-900 mb-8">Raspberry Pi Control Panel</h1>
        
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {raspberryPis.map((pi) => (
            <PiCard
              key={pi.id}
              pi={pi}
              onCommand={(command) => handleCommand(pi.id, command)}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

export default App;