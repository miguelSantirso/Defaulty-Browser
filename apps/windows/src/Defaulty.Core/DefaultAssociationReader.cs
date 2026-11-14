namespace Defaulty.Core;

public sealed record AssociationInfo(
    string Scheme,
    string? ProgId,
    string? ExecutablePath,
    string? FriendlyName,
    string? IconReference,
    string? AppId);

public sealed record CurrentAssociationState(
    AssociationInfo Https,
    AssociationInfo Http,
    bool HttpsResolved,
    bool HttpResolved);

public sealed class DefaultAssociationReader
{
    private readonly IAssocQueryString _assoc;

    public DefaultAssociationReader(IAssocQueryString assoc)
    {
        _assoc = assoc;
    }

    public AssociationInfo ReadScheme(string scheme)
    {
        var flags = AssocF.IsProtocol | AssocF.AppToApp;
        string assoc = scheme;

        string? progId = SafeQuery(flags, AssocStr.ProgId, assoc);
        string? exe = SafeQuery(flags, AssocStr.Executable, assoc);
        string? friendly = SafeQuery(flags, AssocStr.FriendlyAppName, assoc);
        string? icon = SafeQuery(flags, AssocStr.AppIconReference, assoc);
        string? appId = SafeQuery(flags, AssocStr.AppId, assoc);

        bool resolved = !string.IsNullOrWhiteSpace(progId) || !string.IsNullOrWhiteSpace(exe) || !string.IsNullOrWhiteSpace(friendly);

        return new AssociationInfo(scheme, progId, exe, friendly, icon, appId);
    }

    public CurrentAssociationState ReadCurrent()
    {
        var https = ReadScheme("https");
        var http = ReadScheme("http");

        bool httpsResolved = IsResolved(https);
        bool httpResolved = IsResolved(http);

        return new CurrentAssociationState(https, http, httpsResolved, httpResolved);
    }

    public CurrentAssociationState GetCurrent() => ReadCurrent();

    public AssociationInfo ReadAssociation(string scheme) => ReadScheme(scheme);

    private static bool IsResolved(AssociationInfo info)
    {
        return !string.IsNullOrWhiteSpace(info.ProgId)
            || !string.IsNullOrWhiteSpace(info.ExecutablePath)
            || !string.IsNullOrWhiteSpace(info.FriendlyName);
    }

    private string? SafeQuery(AssocF flags, AssocStr str, string assoc)
    {
        try
        {
            return _assoc.Query(flags, str, assoc);
        }
        catch
        {
            return null;
        }
    }
}