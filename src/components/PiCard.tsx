import React from 'react';
import { Monitor, Power, RefreshCw } from 'lucide-react';
import type { RaspberryPi } from '../types/types';

interface PiCardProps {
  pi: RaspberryPi;
  onCommand: (type: 'restart' | 'shutdown' | 'reboot') => void;
}

export function PiCard({ pi, onCommand }: PiCardProps) {
  return (
    <div className="bg-white rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center space-x-3">
          <Monitor className="w-8 h-8 text-purple-600" />
          <div>
            <h3 className="font-semibold text-lg">{pi.name}</h3>
            <p className="text-sm text-gray-500">{pi.ipAddress}</p>
          </div>
        </div>
        <span className={`px-3 py-1 rounded-full text-sm ${
          pi.status === 'online' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
        }`}>
          {pi.status}
        </span>
      </div>
      
      <div className="flex justify-end space-x-2">
        <button
          onClick={() => onCommand('restart')}
          className="p-2 text-purple-600 hover:bg-purple-50 rounded-full transition-colors"
          title="Restart"
        >
          <RefreshCw className="w-5 h-5" />
        </button>
        <button
          onClick={() => onCommand('shutdown')}
          className="p-2 text-red-600 hover:bg-red-50 rounded-full transition-colors"
          title="Shutdown"
        >
          <Power className="w-5 h-5" />
        </button>
      </div>
    </div>
  );
}