namespace InsightBase.Api
{
    public class AnonymousMiddleware // kullanıcı kayıtlı değilse anon... id verir
    {
        private readonly RequestDelegate _next;
        public AnonymousMiddleware(RequestDelegate next) => _next = next;
        public async Task Invoke(HttpContext context)
        {
            const string cookieName = "anon-id";

            if(!context.User.Identity?.IsAuthenticated ?? false) //isAuth
            {
                if(!context.Request.Cookies.TryGetValue(cookieName, out var anonId))
                {
                    anonId = Guid.NewGuid().ToString();
                    context.Response.Cookies.Append(
                        cookieName,
                        anonId,
                        new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = false,
                            SameSite = SameSiteMode.Lax,
                            Expires = DateTimeOffset.UtcNow.AddYears(1)
                        }
                    );
                }
                context.Items["AnonymousId"] = anonId; // Anonymous kullanıcı Id'sini HttpContext.Items'ta tutarak erişilebilir hale getiriyoruz
            }
            else
            {
                // login olduğunda anon-id cookie si siliinir
                if(context.Request.Cookies.ContainsKey(cookieName))
                {
                    context.Response.Cookies.Delete(cookieName);
                    context.Items.Remove("AnonymousId");
                }
            }
            await _next(context);
        }
    }
}