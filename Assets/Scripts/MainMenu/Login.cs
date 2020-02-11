using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Login : MonoBehaviour
{
    // Cached references
    public InputField emailInputField;
    public InputField passwordInputField;
    public Button loginButton;
    public Button logoutButton;
    public Button playGameButton;
    public Text messageBoardText;
    public Player playerManager;

    private string httpServerAddress;

    private void Start()
    {
        httpServerAddress = playerManager.GetHttpServer();
        playerManager = FindObjectOfType<Player>();
    }

    public void OnLoginButtonClicked()
    {
        StartCoroutine(TryLogin());
    }

    private void GetToken()
    {
        UnityWebRequest httpClient = new UnityWebRequest(httpServerAddress + "/Token", "POST");

        WWWForm dataToSend = new WWWForm();
        dataToSend.AddField("grant_type", "password");
        dataToSend.AddField("username", emailInputField.text);
        dataToSend.AddField("password", passwordInputField.text);

        httpClient.uploadHandler = new UploadHandlerRaw(dataToSend.data);
        httpClient.downloadHandler = new DownloadHandlerBuffer();

        httpClient.SetRequestHeader("Accept", "application/json");
        httpClient.certificateHandler = new BypassCertificate();
        httpClient.SendWebRequest();

        while (!httpClient.isDone)
        {
            Task.Delay(10);
        }

        if (httpClient.isNetworkError || httpClient.isHttpError)
        {
            Debug.Log(httpClient.error);
        }
        else
        {
            string jsonResponse = httpClient.downloadHandler.text;
            AuthorizationToken authToken = JsonUtility.FromJson<AuthorizationToken>(jsonResponse);
            playerManager.Token = authToken.access_token;
        }

        httpClient.Dispose();
    }

    private IEnumerator TryLogin()
    {
        if (string.IsNullOrEmpty(playerManager.Token))
        {
            GetToken();
        }

        UnityWebRequest httpClient = new UnityWebRequest(httpServerAddress + "/api/Account/UserId", "GET");

        httpClient.SetRequestHeader("Authorization", "bearer " + playerManager.Token);
        httpClient.SetRequestHeader("Accept", "application/json");

        httpClient.downloadHandler = new DownloadHandlerBuffer();

        httpClient.certificateHandler = new BypassCertificate();
        yield return httpClient.SendWebRequest();

        if (httpClient.isNetworkError || httpClient.isHttpError)
        {
            Debug.Log(httpClient.error);
        }
        else
        {
            playerManager.PlayerId = httpClient.downloadHandler.text;
            messageBoardText.text += "\nWelcome " + playerManager.PlayerId + ". You are logged in!";
            loginButton.interactable = false;
            logoutButton.interactable = true;
            playGameButton.interactable = true;
        }

        httpClient.Dispose();
    }

    public void OnLogoutButtonClicked()
    {
        StartCoroutine(TryLogout());
    }

    private IEnumerator TryLogout()
    {
        UnityWebRequest httpClient = new UnityWebRequest(httpServerAddress + "/api/Account/Logout", "POST");

        httpClient.SetRequestHeader("Authorization", "bearer" + playerManager.Token);

        yield return httpClient.SendWebRequest();

        if (httpClient.isNetworkError || httpClient.isHttpError)
        {
            Debug.Log(httpClient.error);
        }
        else
        {
            messageBoardText.text += $"\n{httpClient.responseCode} Bye bye {playerManager.PlayerId}.";
            playerManager.Token = string.Empty;
            playerManager.PlayerId = string.Empty;
            playerManager.Email = string.Empty;
            loginButton.interactable = true;
            logoutButton.interactable = false;
            playGameButton.interactable = false;
        }
    }
}
