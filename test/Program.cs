using System.Net.Http.Headers;
using System.Diagnostics;
using Newtonsoft.Json;
using Microsoft.Identity.Client;

class Program
{
    static async Task Main(string[] args)
    {
        string clientId = "1d5ff7b9-45cc-4ad4-b295-492e38fa5a20";
        string tenantId = "7f0248ab-73bf-46db-8f24-6a9b581df8ee";
        string redirectUri = "http://localhost"; // Redirect URI configured in Azure AD app
        string[] scopes = { "Files.ReadWrite.All" }; // Scopes required for accessing OneDrive
        string filePath = "C:\\Users\\MaheshMadai\\Downloads\\Employee.xlsx";

        string accessToken = await GetAccessTokenForUserAsync(clientId, tenantId, redirectUri, scopes);

        if (accessToken != null)
        {
            string fileId = await UploadFileToOneDriveAsync(filePath, accessToken);
            if (fileId != null)
            {
                string sharingLink = await GetSharingLinkAsync(fileId, accessToken);
                if (sharingLink != null)
                {
                    OpenInOfficeOnline(sharingLink);
                }
            }
        }
    }
    static async Task<string> GetAccessTokenForUserAsync(string clientId, string tenantId, string redirectUri, string[] scopes)
    {
        try
        {
            var app = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .WithRedirectUri(redirectUri)
                .Build();

            var result = await app.AcquireTokenInteractive(scopes)
                .ExecuteAsync();

            return result.AccessToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting access token: {ex.Message}");
            return null;
        }
    }
    static async Task<string> UploadFileToOneDriveAsync(string filePath, string accessToken)
    {
        try
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                string fileName = Path.GetFileName(filePath);
                string driveId = "b!aVer49dogEiyLjJ5DrzqzBFjHF_v3ftOj2KC2KlmP_H91AVyo9azQbjQBNopudEQ";

                string uploadUrl = $"https://graph.microsoft.com/v1.0/drives/{driveId}/root:/Uploads/{fileName}:/content";

                using (var fileStream = File.OpenRead(filePath))
                {
                    var content = new StreamContent(fileStream);
                    var response = await client.PutAsync(uploadUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var fileData = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                        return fileData.id;
                    }
                    else
                    {
                        Console.WriteLine($"Failed to upload file. Status code: {response.StatusCode}");
                        return null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file: {ex.Message}");
            return null;
        }
    }
    static async Task<string> GetSharingLinkAsync(string fileId, string accessToken)
    {
        try
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                string driveId = "b!aVer49dogEiyLjJ5DrzqzBFjHF_v3ftOj2KC2KlmP_H91AVyo9azQbjQBNopudEQ";

                var response = await client.PostAsync($"https://graph.microsoft.com/v1.0/drives/{driveId}/items/{fileId}/createLink", null);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var sharingLink = JsonConvert.DeserializeObject<dynamic>(jsonResponse).link.webUrl;
                    return sharingLink;
                }
                else
                {
                    Console.WriteLine($"Failed to get sharing link. Status code: {response.StatusCode}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting sharing link: {ex.Message}");
            return null;
        }
    }
    static void OpenInOfficeOnline(string sharingLink)
    {
        Process.Start(new ProcessStartInfo(sharingLink) { UseShellExecute = true });
    }
}

