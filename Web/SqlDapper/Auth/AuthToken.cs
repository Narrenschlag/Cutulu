namespace Cutulu.Web;

using Core;
using System;

public struct AuthToken(long id, string token, long userId, DateTime createdAt, DateTime expiresAt)
{
    [Encodable] public long Id = id;
    [Encodable] public string Token = token;
    [Encodable] public long UserId = userId;
    [Encodable] public DateTime CreatedAt = createdAt;
    [Encodable] public DateTime ExpiresAt = expiresAt;
}