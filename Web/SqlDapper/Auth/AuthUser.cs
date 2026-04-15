namespace Cutulu.Web;

using Core;

public struct AuthUser(long id, string name, string passwordHash)
{
    [Encodable] public long Id = id;
    [Encodable] public string Name = name;
    [Encodable] public string PasswordHash = passwordHash;
}