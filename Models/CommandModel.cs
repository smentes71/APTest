namespace RaspberryPiControl.Models
{
    public class CommandModel
    {
        public CommandType Type { get; set; }
        public string Target { get; set; }

    }

    public enum CommandType
    {
        Restart,
        Shutdown,
        Reboot
    }
    public class CommandRequest
    {
        public string IpAddress { get; set; }
        public string Command { get; set; }
    }

    
}