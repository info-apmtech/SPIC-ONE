namespace SPIC.Core.DTOs
{
    // POST api/Authentication/refresh body.
    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = "";
    }

    // POST api/Authentication/refresh response - same shape as the fields LoginResponseModel
    // already returns for the token pair, so the client can overwrite its stored session with
    // this response the same way it does after a fresh login.
    public class RefreshTokenResponseDto
    {
        public string Token { get; set; } = "";
        public DateTime Expiration { get; set; }
        public string RefreshToken { get; set; } = "";
    }
}
