namespace InsightBase.Application.DTOs.Common
{
    public class Result 
    {
        public bool IsSuccess { get; }
        public string? Message { get; }
        protected Result(bool isSuccess, string? message = null)
        {
            IsSuccess = isSuccess;
            Message = message;
        }
        public static Result Success() => new(true, null);
        public static Result Fail(string? message = null) => new Result(false, message);
    }
}