using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.Security;
using System.Reflection;
using static E.Standard.CMS.Core.CmsDocument;

namespace E.Standard.CMS.Core.Test;

public class CmsDocumentTests
{
    #region IsEqualAuthName

    //  ▶︎  Uncomment this helper if you keep the method private
    private static bool InvokeIsEqualAuthName(string user, string auth, bool strict)
    {
        var mi = typeof(AuthNameExtensions)
                    .GetMethod("IsEqualAuthName",
                            BindingFlags.NonPublic | BindingFlags.Static)!;

        return (bool)mi.Invoke(null, new object?[] { user, auth, strict })!;
    }


    // ────────────────────────────────────────────────────────────────
    // 1.  Simple equality (case‑insensitive) – should always pass.
    // ────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("gr.admins", "Gr.Admins", true)]
    [InlineData("Gr.Admins", "gr.admins", false)]
    public void Returns_true_when_strings_match_ignoring_case(
        string userNameOrRole, string authName, bool strict)
    {
        // Act
        var result = InvokeIsEqualAuthName(userNameOrRole, authName, strict);
        // var result = InvokeIsEqualAuthName(userNameOrRole, authName, strict); // if private

        // Assert
        Assert.True(result);
    }

    // ────────────────────────────────────────────────────────────────
    // 2.  Prefix handling used (strict = false) ⇒ should be true.
    //     Example straight from the XML comment.
    // ────────────────────────────────────────────────────────────────
    [Fact]
    public void Returns_true_when_prefix_matches_and_strict_is_false()
    {
        // Arrange
        const string userNameOrRole = "nt-role::gr.admins";
        const string authName = "Gr.Admins";
        const bool strict = false;

        // Act
        var result = InvokeIsEqualAuthName(userNameOrRole, authName, strict);

        // Assert
        Assert.True(result);
    }

    // ────────────────────────────────────────────────────────────────
    // 3.  Same data as test #2 but strict = true ⇒ should be false.
    // ────────────────────────────────────────────────────────────────
    [Fact]
    public void Returns_false_when_prefix_matches_but_strict_is_true()
    {
        // Arrange
        const string userNameOrRole = "nt-role::gr.admins";
        const string authName = "Gr.Admins";
        const bool strict = true;

        // Act
        var result = InvokeIsEqualAuthName(userNameOrRole, authName, strict);

        // Assert
        Assert.False(result);
    }

    // ────────────────────────────────────────────────────────────────
    // 4.  Prefix present but DOES NOT end with authName ⇒ false.
    // ────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("nt-role::gr.adminsx", "gr.admins")]     // trailing char
    [InlineData("nt-role::gr.admin", "gr.admins")]     // missing char
    [InlineData("nt-role::xx.gr.admins", "gr.admins")]     // extra segment
    public void Returns_false_when_suffix_does_not_match(
        string userNameOrRole, string authName)
    {
        // Act
        var result = InvokeIsEqualAuthName(userNameOrRole, authName, false);

        // Assert
        Assert.False(result);
    }

    // ────────────────────────────────────────────────────────────────
    // 5.  Prefix on both operands ‑‑ the second branch must not run,
    //     result should be based solely on equality.
    // ────────────────────────────────────────────────────────────────
    [Fact]
    public void Returns_false_when_both_values_have_a_prefix_and_are_not_equal()
    {
        // Arrange
        const string userNameOrRole = "nt-role::gr.admins";
        const string authName = "xx-role::gr.admins";

        // Act
        var result = InvokeIsEqualAuthName(userNameOrRole, authName, strict: false);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region HasAuthNamePrefix

    // -------------------------------------------------------------
    //  Reflection‑Invoker für beide Extension‑Methoden
    // -------------------------------------------------------------
    private static bool Invoke(string methodName, string arg)
    {
        var mi = typeof(AuthNameExtensions)    // ← Klassenname ggf. anpassen
                 .GetMethod(methodName,
                            BindingFlags.Static | BindingFlags.NonPublic)!;

        return (bool)mi.Invoke(null, new object?[] { arg })!;
    }

    // -------------------------------------------------------------
    //  HasAuthNamePrefix
    // -------------------------------------------------------------
    [Theory]
    [InlineData("nt-role::gr.admins", true)]   // enthält das "::"‑Präfix
    [InlineData("gr.admins", false)] // kein Präfix
    public void HasAuthNamePrefix_recognises_prefix_correctly(string value, bool expected)
    {
        // Act
        var result = Invoke("HasAuthNamePrefix", value);

        // Assert
        Assert.Equal(expected, result);
    }

    // -------------------------------------------------------------
    //  HasNoAuthNamePrefix
    // -------------------------------------------------------------
    [Theory]
    [InlineData("nt-role::gr.admins", false)]  // hat Präfix  ⇒ false
    [InlineData("gr.admins", true)]  // kein Präfix ⇒ true
    public void HasNoAuthNamePrefix_recognises_absence_of_prefix_correctly(string value, bool expected)
    {
        // Act
        var result = Invoke("HasNoAuthNamePrefix", value);

        // Assert
        Assert.Equal(expected, result);
    }

    // -------------------------------------------------------------
    //  Edge‑Case: empty String
    // -------------------------------------------------------------
    [Fact]
    public void Empty_string_treated_as_no_prefix()
    {
        const string input = "";

        Assert.False(Invoke("HasAuthNamePrefix", input));
        Assert.True(Invoke("HasNoAuthNamePrefix", input));
    }

    #endregion

    #region CheckAuthorization

    // -------------------------------------------------------------
    //  Reflection‑Invoker für CmsDocument.CheckAuthorization
    // -------------------------------------------------------------
    private static bool InvokeCheckAuthorization(UserIdentification ui, AuthNode node)
    {
        MethodInfo mi = typeof(CmsDocument)
                        .GetMethod("CheckAuthorization",
                                   BindingFlags.Static |
                                   BindingFlags.NonPublic |
                                   BindingFlags.Public)!;

        return (bool)mi.Invoke(null, new object?[] { ui, node })!;
    }

    // -------------------------------------------------------------
    //  Mini‑Factories für Testobjekte
    // -------------------------------------------------------------


    private static AuthNode MakeAuthNode(
        IEnumerable<CmsAuthItem> allowedUsers = null!,
        IEnumerable<CmsAuthItem> deniedUsers = null!,
        IEnumerable<CmsAuthItem> allowedRoles = null!,
        IEnumerable<CmsAuthItem> deniedRoles = null!)
    {
        var users = new CmsAuthItemList.UniqueItemListBuilder();
        users.AddRange(allowedUsers?.ToArray() ?? []);
        users.AddRange(deniedUsers?.ToArray() ?? []);

        var roles = new CmsAuthItemList.UniqueItemListBuilder();
        roles.AddRange(allowedRoles?.ToArray() ?? []);
        roles.AddRange(deniedRoles?.ToArray() ?? []);

        return new AuthNode(
            new CmsAuthItemList(users.Build()),
            new CmsAuthItemList(roles.Build()));
    }

    private static UserIdentification MakeUser(
        string name,
        string[] roles = null!,
        string[] instanceRoles = null!)
        => new UserIdentification(name,
                                   roles ?? Array.Empty<string>(),
                                   userrolesParams: null,
                                   instanceRoles ?? Array.Empty<string>(),
                                   publicKey: "",
                                   task: "");


    // -------------------------------------------------------------
    //  1. Null‑Arguments  →  always TRUE
    // -------------------------------------------------------------
    [Fact]
    public void Returns_true_when_ui_is_null()
    {
        var auth = MakeAuthNode();
        Assert.True(InvokeCheckAuthorization(null!, auth));
    }

    [Fact]
    public void Returns_true_when_authNode_is_null()
    {
        var ui = MakeUser("alice");
        Assert.True(InvokeCheckAuthorization(ui, null!));
    }

    // -------------------------------------------------------------
    //  2. „Everyone“ in the Allowed‑User‑Set
    // -------------------------------------------------------------
    [Fact]
    public void Everyone_grants_access()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser(CmsDocument.Everyone, true) }
        );
        var ui = MakeUser("whoever");

        Assert.True(InvokeCheckAuthorization(ui, auth));
    }

    [Fact]
    public void Everyone_disallowed_access()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser(CmsDocument.Everyone, false) }
        );
        var ui = MakeUser("whoever");

        Assert.False(InvokeCheckAuthorization(ui, auth));
    }

    // -------------------------------------------------------------
    //  3. username explicitly allowed/Username not explicitly allowed
    // -------------------------------------------------------------
    [Fact]
    public void Explicit_user_match_grants_access()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser("alice", true) }
        );
        var ui = MakeUser("Alice");      // Groß/klein gemischt
        Assert.True(InvokeCheckAuthorization(ui, auth));
    }

    [Fact]
    public void Explicit_user_match_grants_access_with_prefix()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser("alice", true) }
        );
        var ui = MakeUser("nt-user::Alice");      // Groß/klein gemischt
        Assert.True(InvokeCheckAuthorization(ui, auth));
    }

    [Fact]
    public void Explicit_not_user_match_grants_access_with_prefix()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser("alice", true) }
        );
        var ui = MakeUser("nt-user::bob");      // Groß/klein gemischt
        Assert.False(InvokeCheckAuthorization(ui, auth));
    }

    [Fact]
    public void Explicit_user_match_grants_disallowed_with_prefix()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser("alice", false) }
        );
        var ui = MakeUser("nt-user::Alice");      // Groß/klein gemischt
        Assert.False(InvokeCheckAuthorization(ui, auth));
    }

    [Fact]
    public void Explicit_not_user_match_grants_disallowed_with_prefix()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser("alice", false) }
        );
        var ui = MakeUser("nt-user::bob");      // Groß/klein gemischt
        Assert.False(InvokeCheckAuthorization(ui, auth));
    }

    [Fact]
    public void User_match_with_no_grants_access()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser("alice", true) }
        );
        var ui = MakeUser("bob");      // Groß/klein gemischt
        Assert.False(InvokeCheckAuthorization(ui, auth));
    }

    [Fact]
    public void User_match_with_no_grants_access_with_prefix()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser("alice", true) }
        );
        var ui = MakeUser("nt-user::bob");      // Groß/klein gemischt
        Assert.False(InvokeCheckAuthorization(ui, auth));
    }

    // -------------------------------------------------------------
    //  4. role allowed
    // -------------------------------------------------------------
    [Fact]
    public void Allowed_role_grants_access()
    {
        var auth = MakeAuthNode(
            allowedRoles: new[] { new CmsRole("editors", true) }
        );
        var ui = MakeUser("bob", roles: new[] { "Editors" });
        Assert.True(InvokeCheckAuthorization(ui, auth));
    }

    // -------------------------------------------------------------
    //  5. Instanz‑Role allowd
    // -------------------------------------------------------------
    [Fact]
    public void Allowed_instance_role_grants_access()
    {
        var auth = MakeAuthNode(
            allowedRoles: new[] { new CmsRole("instance::writers", true) }
        );
        var ui = MakeUser("carol",
                          roles: Array.Empty<string>(),
                          instanceRoles: new[] { "writers" });
        Assert.True(InvokeCheckAuthorization(ui, auth));
    }

    // -------------------------------------------------------------
    //  6. user explicitly denied  →  false,
    //     even if user is in allowedRoles
    // -------------------------------------------------------------
    [Fact]
    public void Denied_user_overrides_role_allow()
    {
        var auth = MakeAuthNode(
            allowedRoles: new[] { new CmsRole("editors", true) },
            deniedUsers: new[] { new CmsUser("dave", false) }
        );
        var ui = MakeUser("dave", roles: new[] { "editors" });
        Assert.False(InvokeCheckAuthorization(ui, auth));
    }

    // -------------------------------------------------------------
    //  7. Denied Role (no allowed‑Role)  →  false
    //     Denied Role (allowed‑Role)  →  true
    // -------------------------------------------------------------
    [Fact]
    public void Denied_role_blocks_access_when_not_otherwise_allowed()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser(CmsDocument.Everyone, true) },
            deniedRoles: new[] { new CmsRole("banned", false) }
        );
        var ui = MakeUser("erin", roles: new[] { "banned" });
        Assert.False(InvokeCheckAuthorization(ui, auth));
    }

    [Fact]
    public void Denied_role_dont_blocks_access_when_otherwise_allowed()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser(CmsDocument.Everyone, true) },
            allowedRoles: new[] { new CmsRole("editors", true) },
            deniedRoles: new[] { new CmsRole("banned", false) }
        );
        var ui = MakeUser("erin", roles: new[] { "banned", "editors" });
        Assert.True(InvokeCheckAuthorization(ui, auth));
    }

    [Fact]
    public void Denied_role_dont_blocks_access_when_otherwise_allowed_even_if_everyone_disallowed()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser(CmsDocument.Everyone, false) },
            allowedRoles: new[] { new CmsRole("editors", true) },
            deniedRoles: new[] { new CmsRole("banned", false) }
        );
        var ui = MakeUser("erin", roles: new[] { "banned", "editors" });
        Assert.True(InvokeCheckAuthorization(ui, auth));
    }

    // -------------------------------------------------------------
    //  8. Denied Instanz‑Role (no allowed‑Instanz)  →  false
    //     Denied Instanz‑Role (allowed‑Instanz)  →  true
    // -------------------------------------------------------------
    [Fact]
    public void Denied_instance_role_blocks_access_when_not_otherwise_allowed()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser(CmsDocument.Everyone, true) },
            deniedRoles: new[] { new CmsRole("instance::restricted", false) }
        );
        var ui = MakeUser("frank",
                          instanceRoles: new[] { "restricted" });
        Assert.False(InvokeCheckAuthorization(ui, auth));
    }

    [Fact]
    public void Denied_instance_role_dont_blocks_access_when_otherwise_allowed()
    {
        var auth = MakeAuthNode(
            allowedUsers: new[] { new CmsUser(CmsDocument.Everyone, true) },
            allowedRoles: new[] { new CmsRole("instance::writers", true) },
            deniedRoles: new[] { new CmsRole("instance::restricted", false) }
        );
        var ui = MakeUser("frank",
                          instanceRoles: new[] { "restricted", "instance::writers" });
        Assert.False(InvokeCheckAuthorization(ui, auth));
    }

    #endregion

    #region AuthNode Tests

    private static AuthNode MakeAuthNode(
        IEnumerable<CmsAuthItem> userItems,
        IEnumerable<CmsAuthItem> roleItems)
    {
        var users = new CmsAuthItemList.UniqueItemListBuilder();
        users.AddRange(userItems?.ToArray() ?? []);

        var roles = new CmsAuthItemList.UniqueItemListBuilder();
        roles.AddRange(roleItems?.ToArray() ?? []);

        return new AuthNode(
            new CmsAuthItemList(users.Build()),
            new CmsAuthItemList(roles.Build()));
    }

    /* -----------------------------------------------------------
    *  1. Verify splitting into Items / Allowed / Denied
    * -----------------------------------------------------------
    */
    [Fact]
    public void Users_collections_are_split_into_allowed_and_denied_correctly()
    {
        // 2 allowed + 1 denied user
        var users = new CmsAuthItem[] {
            new CmsUser("alice", true),
            new CmsUser("bob", true),
            new CmsUser("mallory", false)
        };

        // 2 allowed + 1 denied user
        var roles = new CmsAuthItem[] {
            new CmsRole("editors", true),
            new CmsRole("banned", false),
            new CmsRole("readonly", false)
        };

        var node = MakeAuthNode(users, roles);

        // Users
        Assert.Equal(3, node.Users.Items.Count());          // Items = 3
        Assert.Equal(2, node.Users.AllowedItems.Count());   // AllowedItems = 2
        Assert.Single(node.Users.DeniedItems);    // DeniedItems = 1

        // Roles
        Assert.Equal(3, node.Roles.Items.Count());
        Assert.Single(node.Roles.AllowedItems);             // exakt 1 erlaubt
        Assert.Equal(2, node.Roles.DeniedItems.Count());
    }

    /* -----------------------------------------------------------
         *  2. Strict mode is TRUE when every name (except "Everyone")
         *     contains a prefix ("::").
         * -----------------------------------------------------------
         */
    [Fact]
    public void Strict_mode_is_true_when_every_item_has_a_prefix()
    {
        var users = new CmsAuthItem[]
        {
                new CmsUser("nt-user::alice", true),
                new CmsUser(CmsDocument.Everyone, true)  // ignored by the algorithm
        };

        var roles = new CmsAuthItem[]
        {
                new CmsRole("nt-role::editors",  true),
                new CmsRole("nt-role::moderator",false)
        };

        var node = MakeAuthNode(users, roles);

        Assert.True(node.UseScrictAuthNameComparing);
    }

    /* -----------------------------------------------------------
     *  3. Strict mode is FALSE when at least one name lacks a
     *     prefix.
     * -----------------------------------------------------------
     */
    [Fact]
    public void Strict_mode_is_false_when_any_item_lacks_prefix()
    {
        var users = new CmsAuthItem[]
        {
                new CmsUser("alice",           true),   // no "::" prefix
                new CmsUser("nt-user::bob",    true)
        };

        var roles = new CmsAuthItem[]
        {
                new CmsRole("nt-role::editors", true)
        };

        var node = MakeAuthNode(users, roles);

        Assert.False(node.UseScrictAuthNameComparing);
    }

    /* -----------------------------------------------------------
     *  4. Strict mode defaults to TRUE for empty lists
     *     (no item without prefix can exist).
     * -----------------------------------------------------------
     */
    [Fact]
    public void Strict_mode_defaults_to_true_for_empty_node()
    {
        var node = MakeAuthNode(
            userItems: Array.Empty<CmsAuthItem>(),
            roleItems: Array.Empty<CmsAuthItem>());

        Assert.True(node.UseScrictAuthNameComparing);
    }

    #endregion
}
