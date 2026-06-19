using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace SchoolManagement.Api.Services;


public static class FirebaseInitializer
{
    public static bool IsReady { get; private set; }

    public static void Init(IConfiguration config, ILogger logger)
    {
        var path = config["Firebase:ServiceAccountPath"];
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            logger.LogWarning("Firebase service account not found - FCM push disabled (in-app notifications still work)");
            return;
        }

        FirebaseApp.Create(new AppOptions
        {
            Credential = CredentialFactory.FromFile<ServiceAccountCredential>(path).ToGoogleCredential(),
        });
        IsReady = true;
        logger.LogInformation("Firebase Admin initialized");
    }
}
