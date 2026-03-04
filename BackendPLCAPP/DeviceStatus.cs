namespace BackendPLCAPP
{
    public class DeviceStatus
    {
        public bool Di1 { get; set; }
        public bool Di2 { get; set; }
        public bool Di3 { get; set; }
        public bool Di4 { get; set; }
        public string ConnectionState { get; set; } = "Disconnected";
    }
}
