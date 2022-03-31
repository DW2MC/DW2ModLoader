using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using JetBrains.Annotations;
using NuGet.Versioning;
using Octokit;
using Octokit.Internal;
using Xenko.Games;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public class GitHubUpdateCheck : IUpdateCheck
{
    private static readonly GitHubClient Client = new(
        new Connection(new(@"DW2ModLoader-UpdateCheck"), new FancyHttpClientAdapter(() => new HttpClientHandler
            { SslProtocols = (SslProtocols)12288 /* TLS 1.3 */, UseProxy = false })));

    private readonly string _owner;
    private readonly string _name;
    private readonly NuGetVersion _currentVersion;
    private readonly Lazy<Task<bool>> _newVersionCheck;
    private bool? _isNewVersionAvail;

    private static string StripLeadingV(string s)
        => s[0] == 'v' ? s.Substring(1) : s;


    public GitHubUpdateCheck(string repoUri, NuGetVersion currentVersion)
        : this(new Uri(repoUri), currentVersion) { }
    public GitHubUpdateCheck(Uri repoUri, string currentVersion)
        : this(repoUri, NuGetVersion.Parse(StripLeadingV(currentVersion))) { }
    public GitHubUpdateCheck(string repoUri, string currentVersion)
        : this(new Uri(repoUri), NuGetVersion.Parse(StripLeadingV(currentVersion))) { }
    public GitHubUpdateCheck(Uri repoUri, NuGetVersion currentVersion)
    {
        if (repoUri.Scheme != "https")
            throw new NotSupportedException(repoUri.Scheme);
        if (repoUri.Host != "github.com")
            throw new NotSupportedException(repoUri.Host);
        var path = repoUri.PathAndQuery;
        var queryIndex = path.IndexOf('?');
        if (queryIndex > 0)
            path = path.Substring(0, queryIndex);
        var startsWithSlash = path[0] == '/';
        var ownerOffset = startsWithSlash ? 1 : 0;
        var firstSlash = path.IndexOf('/', ownerOffset);
        _owner = path.Substring(ownerOffset, firstSlash - 1);
        _name = path.Substring(firstSlash + 1);
        _currentVersion = currentVersion;
        _newVersionCheck = new(
            () => Task.Run(PerformCheckAsync),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public NuGetVersion? NewVersion { get; private set; }

    public Task<bool> NewVersionCheck => _newVersionCheck.Value;

    public bool IsNewVersionAvailable
    {
        get => _isNewVersionAvail ?? DemandCheck();
        private set => _isNewVersionAvail = value;
    }


    private async Task<bool> PerformCheckAsync()
    {
        if (Debugger.IsAttached)
            return false;

        try
        {
            var latest = await Client.Repository.Release.GetLatest(_owner, _name)
                .ConfigureAwait(false);
            var tagName = latest.TagName;
            var commitish = latest.TargetCommitish;
            if (commitish is not null && commitish.Length != 20)
            {
                var ghCommit = await Client.Repository.Commit.Get(_owner, _name, latest.TargetCommitish);
                commitish = ghCommit.Commit.Sha ?? ghCommit.Commit.Url.Substring(ghCommit.Commit.Url.LastIndexOf('/')+1);
            }
            var versionStr = !tagName.Contains('+') ? $"{tagName}+{commitish}" : tagName;
            var latestSemVer = NuGetVersion.Parse(StripLeadingV(versionStr));
            NewVersion = latestSemVer;
            return IsNewVersionAvailable = _currentVersion < latestSemVer;
        }
        catch (Exception ex)
        {
            ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
            return false;
        }
    }

    private bool DemandCheck()
        => NewVersionCheck.GetAwaiter().GetResult();

    public bool Start() => !NewVersionCheck.IsCompleted;

    public static implicit operator bool(GitHubUpdateCheck check)
        => check.IsNewVersionAvailable;
}
