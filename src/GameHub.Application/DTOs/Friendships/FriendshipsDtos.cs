// src/GameHub.Application/DTOs/Friendships/FriendshipsDtos.cs
namespace GameHub.Application.DTOs.Friendships;

/// <summary>
/// Zaakceptowany znajomy — zwracany przez GET /api/friends.
/// "OtherUser" to ta strona relacji, która NIE jest zalogowanym userem.
/// </summary>
public record FriendDto(
    int      FriendshipId,
    int      UserId,
    string   Username,
    string?  AvatarUrl,
    DateTime FriendsSince);

/// <summary>
/// Oczekujące zaproszenie przychodzące — GET /api/friends/requests.
/// Zalogowany user jest Addressee; Requester to osoba, która zaprosiła.
/// </summary>
public record FriendRequestDto(
    int      FriendshipId,
    int      RequesterId,
    string   RequesterUsername,
    string?  RequesterAvatarUrl,
    DateTime SentAt);