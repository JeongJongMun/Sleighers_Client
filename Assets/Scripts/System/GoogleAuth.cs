using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class GoogleAuth : MonoBehaviour
{

    #region PrivateVariables
    #endregion

    #region PublicMethod
    public void GoogleOAuth()
    {
        _GoogleOAuth();
    }
    #endregion

    #region PrivateMethod
    void Start()
    {
        
    }

    private async void _GoogleOAuth()
    {
        var clientId = SecretLoader.googleAuth.id;
        var clientSecret = SecretLoader.googleAuth.secret;

        var scopes = new[] { GmailService.Scope.GmailReadonly };

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            },
            scopes,
            "user",
            CancellationToken.None);

        var service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Sleighers"
        });

        var profile = await service.Users.GetProfile("me").ExecuteAsync();

        if (!string.IsNullOrEmpty(profile.EmailAddress))
        {
            // ������ �̸��� �ּ� ����
            Debug.Log("�̸��� �ּ� :" + profile.EmailAddress);
            OutGameServerManager.instance.LoginSucc(profile.EmailAddress);
            OutGameUI.instance.panels[0].SetActive(false);  // auth panel
            OutGameUI.instance.panels[1].SetActive(true);   // lobby panel
            OutGameUI.instance.topBar.SetActive(true);

        }
        else
        {
            Debug.LogError("����� �̸��� �ּҸ� �������µ� �����߽��ϴ�.");
        }
    }

    #endregion
}
