
using System.IO.Compression;
using System.Reactive.Linq;
using System.Text;
using System.Xml.Linq;
using Octokit;
using Octokit.Internal;
using Octokit.Reactive;


var anglerUpdater = new AnglerUpdater();

await anglerUpdater.DoStuffAsync();


public class AnglerUpdater
{
    static string owner = "agents";
    static string name = "angler";
    static string token = "<your token here>";
    static string baseAddress = "https://source.datanerd.us";
    static InMemoryCredentialStore credentials = new InMemoryCredentialStore(new Credentials(token));
    static ObservableGitHubClient client = new ObservableGitHubClient(new ProductHeaderValue("DotnetAnglerUpdater"), credentials, new Uri(baseAddress));

    public async Task DoStuffAsync()
    {
        // get latest commit for master
        var masterRef = await client.Git.Reference.Get(owner, name, "heads/master");
        var masterSha = masterRef.Object.Sha;
        var latestCommit = await client.Git.Commit.Get(owner, name, masterSha);

        var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            var metricNamesTxt = Path.Combine(tempFolder, @"src\main\resources\metric_names.txt");
            string relativePath = Path.GetRelativePath(tempFolder, metricNamesTxt).Replace("\\", "/");
            var fullPath = Path.GetDirectoryName(metricNamesTxt);
            Directory.CreateDirectory(fullPath!);

            // download the file we're modifying
            var content = await client.Repository.Content.GetAllContentsByRef(owner, name, latestCommit.Sha, "src/main/resources/metric_names.txt");

            // write it to a file
            await File.WriteAllTextAsync(metricNamesTxt, content.Content);
            // update the agent version (which re-writes the file)
            var agentVersion = "10.25.2.0";
            await AddNewAgentVersion(metricNamesTxt, agentVersion);

            // read the modified file into a string
            var updatedText = await File.ReadAllTextAsync(metricNamesTxt);

            // create a new branch
            var branch = $"dotnet-patch-{token.GetHashCode()}";
            var branchRef = await client.Git.Reference.Create(owner, name, new NewReference($"refs/heads/{branch}", masterSha));

            // update the file in the new branch
            var changeSet = await client.Repository.Content.UpdateFile(owner, name, content.Path, new UpdateFileRequest($"chore: Add supportability metric for .NET agent version {agentVersion}", updatedText, content.Sha, branch));

            // create a pull request
            var pullRequest = new NewPullRequest($"chore: Add supportability metric for .NET agent version {agentVersion}",
                branch, "master")
            {
                Body = "This PR adds a supportability metric for the latest .NET agent version.",
                Draft = true, 
            };
            var pr = await client.PullRequest.Create(owner, name, pullRequest);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Oops... {e}");
        }

        finally
        {
            // delete the repo folder
            Directory.Delete(tempFolder, true);
        }
    }

    private async Task AddNewAgentVersion(string filePath, string newVersion)
    {
        var metricPrefix = "Supportability/AgentVersion/";

        // Check if the file exists
        if (!File.Exists(filePath))
        {
            throw new Exception($"File {filePath} not found.");
        }

        // Read all lines into a list
        List<string> lines = new List<string>(await File.ReadAllLinesAsync(filePath));

        // Find the line containing the search string and insert the new string in the next line
        var found = false;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith(metricPrefix))
            {
                lines.Insert(i, $"{metricPrefix}{newVersion}");
                found = true;
                break;
            }
        }

        if (!found)
            throw new Exception("Couldn't find the metric prefix {metricPrefix} in {filePath}.");

        var sb = new StringBuilder();
        foreach(var line in lines)
        {
            sb.Append(line + "\n"); // can't use Environment.NewLine because the file is read on a linux machine
        }

        // Write the modified lines back to the file
        await File.WriteAllTextAsync(filePath, sb.ToString());
    }


    private async Task<string> CloneRepoAsync(string sha)
    {
        var url = $"{baseAddress}/{owner}/{name}/archive/{sha}.zip";

        var tempDir = Path.Combine(Path.GetTempPath(), "angler-updater");
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);

        Directory.CreateDirectory(tempDir);

        using var httpClient = new HttpClient();

        var creds = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:", token);
        creds = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(creds));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", creds);
        var stream = await httpClient.GetStreamAsync(url);
        ZipFile.ExtractToDirectory(stream, tempDir);

        // get the folder that the repo was extracted to
        var extractedFolder = Directory.GetDirectories(tempDir).First();

        return extractedFolder;
    }


}
