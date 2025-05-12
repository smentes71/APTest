export interface RaspberryPi {
  id: string;
  name: string;
  status: 'online' | 'offline';
  ipAddress: string;
}

export interface Command {
  type: 'restart' | 'shutdown' | 'reboot';
  target: string; // Raspberry Pi ID
}