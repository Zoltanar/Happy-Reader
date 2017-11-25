namespace Happy_Apps_Core
{
    /// <summary>
    /// From any command when command fails
    /// </summary>
    public class ErrorResponse
    {
        public double Fullwait { get; set; }
        public string Type { get; set; }
        public string ID { get; set; }
        public string Msg { get; set; }
        public double Minwait { get; set; }
    }
}