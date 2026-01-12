namespace InsightBase.Application.DTOs.Auth
{
    public class AuthResponse
    {
        public string? Token { get; set; } = default!;
        public string? RefreshToken { get; set; } = default!;
        public bool IsSuccess { get; private set; }
        public List<string> Errors { get; private set; }

        public static AuthResponse Success(string? token, string? refreshToken)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token boş olamaz", nameof(token));
            
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentException("RefreshToken boş olamaz", nameof(refreshToken));

            return new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                IsSuccess = true,
                Errors = new List<string>()
            };
        }
        public static AuthResponse Fail(IEnumerable<string> errors)
        {
            var errorList = errors?.ToList() ?? new List<string>();
            
            if (!errorList.Any())
                throw new ArgumentException("En az bir hata mesajı gerekli", nameof(errors));

            return new AuthResponse
            {
                Token = string.Empty,
                RefreshToken = string.Empty,
                IsSuccess = false,
                Errors = errorList
            };
        }

        public static AuthResponse Fail(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                throw new ArgumentException("Hata mesajı boş olamaz", nameof(error));

            return Fail(new List<string> { error });
        }
    }
}