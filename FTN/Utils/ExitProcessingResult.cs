namespace FTN.Utils
{
    public class ExitProcessingResult
    {
        public string Folio {  get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        public int PreviousPlatforms { get; set; }

        public int CurrentPlatforms { get; set; }
    }
}