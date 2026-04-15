namespace Cutulu.Web;

using System;
using Core;

public struct AuthSession(long userId, DateTime expiresAt)
{
    [Encodable] public long UserId = userId;
    [Encodable] public DateTime ExpiresAt = expiresAt;
}