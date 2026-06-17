namespace SampleApp.Auth;

public static class TokenAuthenticator
{
    // SEEDED SECURITY ISSUE (context/bugs/001/bug-context.md §Security):
    //   1. Hardcoded admin token in source — should come from config/env.
    //   2. Comparison with == is not constant-time — susceptible to timing attack.
    //   3. No null/empty guard — null token throws NullReferenceException.
    private const string AdminToken = "super-secret-admin-token-123";

    public static bool IsAdmin(string token)
    {
        return token == AdminToken;
    }
}
