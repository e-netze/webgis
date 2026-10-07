using E.Standard.Cms.Git.Exceptions;
using E.Standard.Cms.Git.Models;
using E.Standard.Cms.Git.Services;

using LibGit2Sharp;

namespace E.Standard.Cms.Git.Tests;

public sealed class CmsGitWorkspaceTests : IDisposable
{
    private readonly string _root;
    private readonly string _remote;
    private readonly string _tree;
    private readonly CmsGitSettings _settings;
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    private static readonly CmsGitUser Alice = new("alice", "alice@test.local");
    private static readonly CmsGitUser Bob = new("bob", "bob@test.local");

    public CmsGitWorkspaceTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        _output = output;
        _root = Path.Combine(Path.GetTempPath(), "cmsgit-tests", Guid.NewGuid().ToString("N"));
        _remote = Path.Combine(_root, "remote.git");
        _tree = Path.Combine(_root, "tree");

        Repository.Init(_remote, isBare: true);

        Directory.CreateDirectory(Path.Combine(_tree, "services"));
        File.WriteAllText(Path.Combine(_tree, "services", "a.xml"), "<a/>");
        File.WriteAllText(Path.Combine(_tree, ".itemorder.xml"), "<order/>");
        File.WriteAllText(Path.Combine(_tree, ".versioninfo.xml"), "<v/>");

        _settings = new CmsGitSettings(_remote, null, null, "main", "cms-bot", "cms-bot@test.local");
    }

    public void Dispose()
    {
        try { CmsGitWorkspace.DeleteDirectory(_root); } catch { }
    }

    private CmsGitWorkspace Workspace(string name) => new(Path.Combine(_root, "ws", name), _settings);

    private CmsGitWorkspace CreateWorkspace(string name, CmsGitUser user)
    {
        var ws = Workspace(name);
        ws.Create(_tree, user, "initial");
        return ws;
    }

    [Fact]
    public void Create_EmptyRemote_PushesInitialTree()
    {
        var ws = CreateWorkspace("alice", Alice);

        Assert.True(ws.Exists);
        Assert.True(File.Exists(Path.Combine(ws.WorkspacePath, "services", "a.xml")));

        var status = ws.GetStatus(true);
        Assert.Equal("main", status.Branch);
        Assert.True(status.IsDefaultBranch);
        Assert.True(status.HasUpstream);
        Assert.Equal(0, status.Ahead);
        Assert.Empty(status.Changes);

        using var remote = new Repository(_remote);
        var tip = remote.Branches["main"].Tip;
        Assert.NotNull(tip);
        Assert.NotNull(tip["services/a.xml"]);
        Assert.Null(tip[".versioninfo.xml"]); // ignored
        Assert.Equal("alice", tip.Author.Name);
        Assert.Equal("cms-bot", tip.Committer.Name);
    }

    [Fact]
    public void Create_ExistingRemote_Clones()
    {
        CreateWorkspace("alice", Alice);
        var bob = CreateWorkspace("bob", Bob);

        Assert.Equal("main", bob.GetStatus(false).Branch);
        Assert.True(File.Exists(Path.Combine(bob.WorkspacePath, "services", "a.xml")));
    }

    [Fact]
    public void Commit_RequiresMessageAndChanges()
    {
        var ws = CreateWorkspace("alice", Alice);

        Assert.Equal(CmsGitErrors.NothingToCommit, Assert.Throws<CmsGitException>(() => ws.Commit(Alice, "x")).L10nKey);

        File.WriteAllText(Path.Combine(ws.WorkspacePath, "services", "b.xml"), "<b/>");
        Assert.Equal(CmsGitErrors.MessageRequired, Assert.Throws<CmsGitException>(() => ws.Commit(Alice, " ")).L10nKey);

        var status = ws.GetStatus(false);
        Assert.Contains(status.Changes, c => c.Path == "services/b.xml" && c.State == CmsGitChangeStates.Added);

        ws.Commit(Alice, "add b");
        status = ws.GetStatus(false);
        Assert.Empty(status.Changes);
        Assert.Equal(1, status.Ahead);
    }

    [Fact]
    public void Push_Then_Pull_OtherWorkspace()
    {
        var alice = CreateWorkspace("alice", Alice);
        var bob = CreateWorkspace("bob", Bob);

        File.WriteAllText(Path.Combine(alice.WorkspacePath, "services", "b.xml"), "<b/>");
        alice.Commit(Alice, "add b");
        alice.Push(Alice);
        Assert.Equal(0, alice.GetStatus(false).Ahead);

        Assert.Equal(1, bob.GetStatus(true).Behind);
        bob.Pull(Bob);
        Assert.True(File.Exists(Path.Combine(bob.WorkspacePath, "services", "b.xml")));
        Assert.Equal(0, bob.GetStatus(false).Behind);
    }

    [Fact]
    public void Pull_RequiresCleanWorkspace()
    {
        var ws = CreateWorkspace("alice", Alice);
        File.WriteAllText(Path.Combine(ws.WorkspacePath, "services", "b.xml"), "<b/>");

        Assert.Equal(CmsGitErrors.CommitFirst, Assert.Throws<CmsGitException>(() => ws.Pull(Alice)).L10nKey);
    }

    [Fact]
    public void Push_Rejected_MergesNonConflictingAndPushes()
    {
        var alice = CreateWorkspace("alice", Alice);
        var bob = CreateWorkspace("bob", Bob);

        File.WriteAllText(Path.Combine(alice.WorkspacePath, "services", "b.xml"), "<b/>");
        alice.Commit(Alice, "add b");
        alice.Push(Alice);

        File.WriteAllText(Path.Combine(bob.WorkspacePath, "services", "c.xml"), "<c/>");
        bob.Commit(Bob, "add c");
        bob.Push(Bob);

        using var remote = new Repository(_remote);
        var tip = remote.Branches["main"].Tip;
        Assert.Equal(2, tip.Parents.Count()); // merge commit
        Assert.NotNull(tip["services/b.xml"]);
        Assert.NotNull(tip["services/c.xml"]);
    }

    private (CmsGitWorkspace alice, CmsGitWorkspace bob) ConflictOnA()
    {
        var alice = CreateWorkspace("alice", Alice);
        var bob = CreateWorkspace("bob", Bob);

        File.WriteAllText(Path.Combine(alice.WorkspacePath, "services", "a.xml"), "<a alice=\"1\"/>");
        alice.Commit(Alice, "alice");
        alice.Push(Alice);

        File.WriteAllText(Path.Combine(bob.WorkspacePath, "services", "a.xml"), "<a bob=\"1\"/>");
        bob.Commit(Bob, "bob");

        Assert.Equal(CmsGitErrors.MergeConflicts, Assert.Throws<CmsGitException>(() => bob.Push(Bob)).L10nKey);

        return (alice, bob);
    }

    [Fact]
    public void Push_Rejected_WithConflict_KeepsMergeStateWithOwnFileVersion()
    {
        var (_, bob) = ConflictOnA();

        var status = bob.GetStatus(false);
        Assert.True(status.IsMerging);
        Assert.Equal("main", status.MergeSource);
        Assert.Equal(1, status.ConflictCount);
        // working copy keeps valid xml (own version, no conflict markers)
        Assert.Equal("<a bob=\"1\"/>", File.ReadAllText(Path.Combine(bob.WorkspacePath, "services", "a.xml")));

        var conflict = Assert.Single(bob.GetConflicts());
        Assert.Equal("services/a", conflict.NodePath);
        Assert.Equal(CmsGitConflictKinds.Modified, conflict.Kind);
        var file = Assert.Single(conflict.Files);
        Assert.Equal("<a bob=\"1\"/>", file.Mine);
        Assert.Equal("<a alice=\"1\"/>", file.Theirs);

        // locked while merging
        Assert.Equal(CmsGitErrors.MergeInProgress, Assert.Throws<CmsGitException>(() => bob.Push(Bob)).L10nKey);
        Assert.Equal(CmsGitErrors.ConflictsRemaining, Assert.Throws<CmsGitException>(() => bob.CompleteMerge(Bob)).L10nKey);
    }

    [Theory]
    [InlineData(CmsGitConflictChoices.Mine, "<a bob=\"1\"/>")]
    [InlineData(CmsGitConflictChoices.Theirs, "<a alice=\"1\"/>")]
    public void Conflict_Resolve_Complete_Push(string choice, string expected)
    {
        var (_, bob) = ConflictOnA();

        bob.ResolveConflict("/services/a", choice);
        Assert.Empty(bob.GetConflicts());

        bob.CompleteMerge(Bob);
        var status = bob.GetStatus(false);
        Assert.False(status.IsMerging);
        Assert.Empty(status.Changes);
        Assert.Equal(expected, File.ReadAllText(Path.Combine(bob.WorkspacePath, "services", "a.xml")));

        bob.Push(Bob);

        using var remote = new Repository(_remote);
        var tip = remote.Branches["main"].Tip;
        Assert.Equal(2, tip.Parents.Count());
        Assert.Equal(expected, ((Blob)tip["services/a.xml"].Target).GetContentText());
        Assert.Equal("bob", tip.Author.Name);
    }

    [Fact]
    public void Conflict_Abort_RestoresLastCommit()
    {
        var (_, bob) = ConflictOnA();

        bob.AbortMerge();

        var status = bob.GetStatus(false);
        Assert.False(status.IsMerging);
        Assert.Empty(status.Changes);
        Assert.Equal(1, status.Ahead);
        Assert.Equal("<a bob=\"1\"/>", File.ReadAllText(Path.Combine(bob.WorkspacePath, "services", "a.xml")));
        Assert.Equal(CmsGitErrors.NotMerging, Assert.Throws<CmsGitException>(() => bob.AbortMerge()).L10nKey);
    }

    [Theory]
    [InlineData(CmsGitConflictChoices.Mine, false)]
    [InlineData(CmsGitConflictChoices.Theirs, true)]
    public void Conflict_DeletedFolder_RestoresOrDeletesWholeSubTree(string choice, bool exists)
    {
        // tree with a folder node "services/f" (metadata + 2 children)
        var alice = CreateWorkspace("alice", Alice);
        var folder = Path.Combine(alice.WorkspacePath, "services", "f");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, ".general.xml"), "<g/>");
        File.WriteAllText(Path.Combine(folder, "x.xml"), "<x/>");
        File.WriteAllText(Path.Combine(folder, "y.xml"), "<y/>");
        alice.Commit(Alice, "folder");
        alice.Push(Alice);

        var bob = CreateWorkspace("bob", Bob);

        // alice modifies a child, bob deletes the whole folder
        File.WriteAllText(Path.Combine(folder, "x.xml"), "<x alice=\"1\"/>");
        alice.Commit(Alice, "modify x");
        alice.Push(Alice);

        CmsGitWorkspace.DeleteDirectory(Path.Combine(bob.WorkspacePath, "services", "f"));
        bob.Commit(Bob, "delete f");
        Assert.Equal(CmsGitErrors.MergeConflicts, Assert.Throws<CmsGitException>(() => bob.Pull(Bob)).L10nKey);

        var conflict = Assert.Single(bob.GetConflicts());
        Assert.Equal("services/f", conflict.NodePath);
        Assert.Equal(CmsGitConflictKinds.DeletedByMe, conflict.Kind);

        bob.ResolveConflict("services/f", choice);
        bob.CompleteMerge(Bob);

        var bobFolder = Path.Combine(bob.WorkspacePath, "services", "f");
        Assert.Equal(exists, Directory.Exists(bobFolder));
        if (exists)
        {
            Assert.Equal("<g/>", File.ReadAllText(Path.Combine(bobFolder, ".general.xml")));
            Assert.Equal("<x alice=\"1\"/>", File.ReadAllText(Path.Combine(bobFolder, "x.xml")));
            Assert.Equal("<y/>", File.ReadAllText(Path.Combine(bobFolder, "y.xml")));
        }
        Assert.Empty(bob.GetStatus(false).Changes);
    }

    [Fact]
    public void Conflict_ItemOrder_IsResolvedAutomatically()
    {
        var alice = CreateWorkspace("alice", Alice);
        var bob = CreateWorkspace("bob", Bob);

        File.WriteAllText(Path.Combine(alice.WorkspacePath, "services", "b.xml"), "<b/>");
        File.WriteAllText(Path.Combine(alice.WorkspacePath, "services", ".itemorder.xml"), "<items><item name=\"a\"/><item name=\"b\"/></items>");
        alice.Commit(Alice, "add b");
        alice.Push(Alice);

        File.WriteAllText(Path.Combine(bob.WorkspacePath, "services", "c.xml"), "<c/>");
        File.WriteAllText(Path.Combine(bob.WorkspacePath, "services", ".itemorder.xml"), "<items><item name=\"c\"/><item name=\"a\"/></items>");
        bob.Commit(Bob, "add c");
        bob.Push(Bob);

        var status = bob.GetStatus(false);
        Assert.False(status.IsMerging);
        Assert.Equal(0, status.Ahead);

        var doc = new System.Xml.XmlDocument();
        doc.Load(Path.Combine(bob.WorkspacePath, "services", ".itemorder.xml"));
        var names = doc.SelectNodes("items/item")!.Cast<System.Xml.XmlNode>().Select(n => n.Attributes!["name"]!.Value).ToArray();
        Assert.Equal(new[] { "c", "a", "b" }, names);
    }

    [Fact]
    public void MergeFromDefault_BringsMainIntoBranch()
    {
        var alice = CreateWorkspace("alice", Alice);
        var bob = CreateWorkspace("bob", Bob);

        Assert.Equal(CmsGitErrors.AlreadyOnDefaultBranch, Assert.Throws<CmsGitException>(() => bob.MergeFromDefault(Bob)).L10nKey);

        bob.CreateBranch("bob/work");

        File.WriteAllText(Path.Combine(alice.WorkspacePath, "services", "b.xml"), "<b/>");
        alice.Commit(Alice, "add b");
        alice.Push(Alice);

        Assert.True(bob.MergeFromDefault(Bob));
        Assert.True(File.Exists(Path.Combine(bob.WorkspacePath, "services", "b.xml")));
        Assert.Equal("bob/work", bob.GetStatus(false).Branch);
    }

    [Fact]
    public void MergeIntoDefault_MergesPushesAndDeletesBranch()
    {
        var alice = CreateWorkspace("alice", Alice);
        var bob = CreateWorkspace("bob", Bob);

        bob.CreateBranch("bob/work");
        File.WriteAllText(Path.Combine(bob.WorkspacePath, "services", "c.xml"), "<c/>");
        bob.Commit(Bob, "add c");

        // main moved in the meantime
        File.WriteAllText(Path.Combine(alice.WorkspacePath, "services", "b.xml"), "<b/>");
        alice.Commit(Alice, "add b");
        alice.Push(Alice);

        bob.MergeIntoDefault(Bob, deleteBranch: true);

        var status = bob.GetStatus(false);
        Assert.Equal("main", status.Branch);
        Assert.Equal(0, status.Ahead);
        Assert.False(status.IsMerging);
        Assert.True(File.Exists(Path.Combine(bob.WorkspacePath, "services", "b.xml")));
        Assert.True(File.Exists(Path.Combine(bob.WorkspacePath, "services", "c.xml")));
        Assert.DoesNotContain(bob.GetBranches(), b => b.Name == "bob/work");

        using var remote = new Repository(_remote);
        Assert.Null(remote.Branches["bob/work"]);
        var tip = remote.Branches["main"].Tip;
        Assert.Equal(2, tip.Parents.Count());
        Assert.NotNull(tip["services/b.xml"]);
        Assert.NotNull(tip["services/c.xml"]);
        Assert.Contains("bob/work", tip.Message);
        Assert.DoesNotContain("refs/", tip.Message);
    }

    [Fact]
    public void MergeIntoDefault_WithConflict_StopsOnBranch()
    {
        var alice = CreateWorkspace("alice", Alice);
        var bob = CreateWorkspace("bob", Bob);

        bob.CreateBranch("bob/work");
        File.WriteAllText(Path.Combine(bob.WorkspacePath, "services", "a.xml"), "<a bob=\"1\"/>");
        bob.Commit(Bob, "bob");

        File.WriteAllText(Path.Combine(alice.WorkspacePath, "services", "a.xml"), "<a alice=\"1\"/>");
        alice.Commit(Alice, "alice");
        alice.Push(Alice);

        Assert.Equal(CmsGitErrors.MergeConflicts, Assert.Throws<CmsGitException>(() => bob.MergeIntoDefault(Bob, true)).L10nKey);
        var status = bob.GetStatus(false);
        Assert.Equal("bob/work", status.Branch);
        Assert.True(status.IsMerging);

        bob.ResolveConflict("*", CmsGitConflictChoices.Mine);
        bob.CompleteMerge(Bob);
        bob.MergeIntoDefault(Bob, true);

        using var remote = new Repository(_remote);
        Assert.Equal("<a bob=\"1\"/>", ((Blob)remote.Branches["main"].Tip["services/a.xml"].Target).GetContentText());
    }

    [Fact]
    public void Discard_RevertsModifiedDeletedAndRemovesNewNodes()
    {
        var ws = CreateWorkspace("alice", Alice);
        var services = Path.Combine(ws.WorkspacePath, "services");

        File.WriteAllText(Path.Combine(services, "a.xml"), "<a changed=\"1\"/>");
        Directory.CreateDirectory(Path.Combine(services, "n"));
        File.WriteAllText(Path.Combine(services, "n.xml"), "<n/>");
        File.WriteAllText(Path.Combine(services, "n", ".general.xml"), "<g/>");
        Assert.Equal(3, ws.GetStatus(false).Changes.Count());

        Assert.True(ws.Discard("/services/a"));
        Assert.Equal("<a/>", File.ReadAllText(Path.Combine(services, "a.xml")));

        Assert.True(ws.Discard("services/n"));
        Assert.False(File.Exists(Path.Combine(services, "n.xml")));
        Assert.False(Directory.Exists(Path.Combine(services, "n")));
        Assert.Empty(ws.GetStatus(false).Changes);
        Assert.False(ws.Discard("services/n"));

        File.Delete(Path.Combine(services, "a.xml"));
        Assert.True(ws.Discard(""));
        Assert.True(File.Exists(Path.Combine(services, "a.xml")));

        Assert.Equal(CmsGitErrors.InvalidPath, Assert.Throws<CmsGitException>(() => ws.Discard("../x")).L10nKey);
    }

    [Theory]
    [InlineData("a/b.xml", "a/b")]
    [InlineData("a/b.acl", "a/b")]
    [InlineData("a/b/.general.xml", "a/b")]
    [InlineData(".itemorder.xml", "")]
    public void NodeOf_MapsFilesToNodes(string path, string node)
        => Assert.Equal(node, CmsGitWorkspace.NodeOf(path));

    [Fact]
    public void Branches_Create_Checkout_Delete()
    {
        var alice = CreateWorkspace("alice", Alice);
        var bob = CreateWorkspace("bob", Bob);

        Assert.Equal(CmsGitErrors.InvalidBranchName, Assert.Throws<CmsGitException>(() => alice.CreateBranch("bad name..")).L10nKey);

        // uncommitted changes are carried into the new branch
        File.WriteAllText(Path.Combine(alice.WorkspacePath, "services", "b.xml"), "<b/>");
        Assert.True(alice.CreateBranch("alice/feature"));
        Assert.Equal("alice/feature", alice.GetStatus(false).Branch);
        Assert.Single(alice.GetStatus(false).Changes);

        Assert.Equal(CmsGitErrors.BranchExists, Assert.Throws<CmsGitException>(() => alice.CreateBranch("alice/feature")).L10nKey);
        Assert.Equal(CmsGitErrors.CommitFirst, Assert.Throws<CmsGitException>(() => alice.Checkout("main")).L10nKey);

        alice.Commit(Alice, "feature");
        alice.Push(Alice);

        // bob sees the remote branch and can check it out
        bob.Fetch();
        Assert.Contains(bob.GetBranches(), b => b.Name == "alice/feature" && b.IsRemote && !b.IsLocal);
        bob.Checkout("alice/feature");
        Assert.True(File.Exists(Path.Combine(bob.WorkspacePath, "services", "b.xml")));
        Assert.True(bob.GetStatus(false).HasUpstream);
        bob.Checkout("main");
        Assert.False(File.Exists(Path.Combine(bob.WorkspacePath, "services", "b.xml")));

        Assert.Equal(CmsGitErrors.CannotDeleteDefaultBranch, Assert.Throws<CmsGitException>(() => alice.DeleteBranch("main", false)).L10nKey);
        Assert.Equal(CmsGitErrors.CannotDeleteCurrentBranch, Assert.Throws<CmsGitException>(() => alice.DeleteBranch("alice/feature", false)).L10nKey);

        alice.Checkout("main");
        alice.DeleteBranch("alice/feature", true);

        using var remote = new Repository(_remote);
        Assert.Null(remote.Branches["alice/feature"]);
        Assert.DoesNotContain(alice.GetBranches(), b => b.Name == "alice/feature");
    }

    [Fact]
    public void Remote_Offline_StatusIsStale()
    {
        var ws = CreateWorkspace("alice", Alice);
        Directory.Move(_remote, _remote + ".offline");

        var status = ws.GetStatus(true);
        Assert.True(status.Stale);
        Assert.False(String.IsNullOrEmpty(status.FetchError));

        // local work continues
        File.WriteAllText(Path.Combine(ws.WorkspacePath, "services", "b.xml"), "<b/>");
        ws.Commit(Alice, "offline commit");
    }

    [Fact]
    public void DeployWorkspace_FollowsRemoteDefaultBranch()
    {
        var alice = CreateWorkspace("alice", Alice);
        var deploy = Workspace("deploy");

        var sha1 = deploy.UpdateToRemoteDefaultBranch();
        Assert.True(File.Exists(Path.Combine(deploy.WorkspacePath, "services", "a.xml")));

        // local garbage in deploy clone is removed
        File.WriteAllText(Path.Combine(deploy.WorkspacePath, "services", "junk.xml"), "<j/>");

        File.WriteAllText(Path.Combine(alice.WorkspacePath, "services", "b.xml"), "<b/>");
        alice.Commit(Alice, "add b");
        alice.Push(Alice);

        var sha2 = deploy.UpdateToRemoteDefaultBranch();
        Assert.NotEqual(sha1, sha2);
        Assert.True(File.Exists(Path.Combine(deploy.WorkspacePath, "services", "b.xml")));
        Assert.False(File.Exists(Path.Combine(deploy.WorkspacePath, "services", "junk.xml")));
    }

    [Fact]
    public void DeployInfo_HeadAndRemoteCommits()
    {
        var deploy = Workspace("deploy");
        Assert.Null(deploy.HeadCommit());

        var alice = CreateWorkspace("alice", Alice);
        var initialSha = alice.HeadCommit().Sha;
        Assert.Equal(initialSha, alice.RemoteDefaultBranchSha());

        deploy.UpdateToRemoteDefaultBranch();
        Assert.Equal(initialSha, deploy.HeadCommit().Sha);

        File.WriteAllText(Path.Combine(alice.WorkspacePath, "services", "b.xml"), "<b/>");
        alice.Commit(Alice, "add b");
        alice.Push(Alice);

        var remoteHead = alice.RemoteDefaultBranchCommit();
        Assert.NotEqual(initialSha, remoteHead.Sha);
        Assert.Equal(remoteHead.Sha, alice.RemoteDefaultBranchSha());
        Assert.Equal("alice", remoteHead.Author);
        Assert.Equal("add b", remoteHead.Message);
        Assert.Equal(8, remoteHead.ShortSha.Length);

        // deploy clone keeps the last deployed state until the next deployment
        Assert.Equal(initialSha, deploy.HeadCommit().Sha);
    }

    [Fact]
    public void Status_LargeTree_Performance()
    {
        var tree = Path.Combine(_root, "large-tree");
        for (var folder = 0; folder < 100; folder++)
        {
            var dir = Path.Combine(tree, "services", $"service{folder}", "themes");
            Directory.CreateDirectory(dir);
            for (var file = 0; file < 50; file++)
            {
                File.WriteAllText(Path.Combine(dir, $"theme{file}.xml"), $"<theme id='{folder}-{file}' />");
            }
        }

        var ws = Workspace("large");
        ws.Create(tree, Alice, "initial");

        File.WriteAllText(Path.Combine(ws.WorkspacePath, "services", "service5", "themes", "theme7.xml"), "<changed/>");

        var watch = System.Diagnostics.Stopwatch.StartNew();
        CmsGitStatus status = null;
        for (var i = 0; i < 5; i++)
        {
            status = ws.GetStatus(false);
        }
        watch.Stop();

        _output.WriteLine($"GetStatus on 5000 files: {watch.ElapsedMilliseconds / 5} ms (avg of 5)");

        Assert.Single(status.Changes);
        Assert.True(watch.ElapsedMilliseconds / 5 < 3000, $"GetStatus too slow: {watch.ElapsedMilliseconds / 5} ms");
    }
}
