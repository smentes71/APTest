using RaspberryPiControl.Models;

namespace RaspberryPiControl.Services
{
    public interface IRaspberryPiService
    {
        Task<IEnumerable<RaspberryPi>> GetAllDevicesAsync();
        //Task<bool> SendCommandAsync(string piId, CommandType command);
        Task<bool> SendCommandAsync(string ipAddress, CommandType command);
        Task<RaspberryPi> GetDeviceByIdAsync(string id);
    }
}