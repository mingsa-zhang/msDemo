namespace CoreDemo2.Models
{
    public class StringOperationRequest
    {
        public string Input { get; set; }
    }

    public class StringOperationResponse
    {
        public string Input { get; set; }
        public string Output { get; set; }
        public string Operation { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}