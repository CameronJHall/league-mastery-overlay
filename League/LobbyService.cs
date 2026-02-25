using league_mastery_overlay.State;

namespace league_mastery_overlay.League;

/// <summary>
/// Fetches the current lobby shape and cross-references members against the provided
/// friends cache (keyed by puuid). The friends list is NOT fetched here — it is
/// supplied by the caller (fetched once on client connect).
/// Stats and Title on each LobbyFriend are always null; the caller fills those in.
/// </summary>
internal sealed class LobbyService(LcuClient client)
{
    public async Task<LobbyState?> PollAsync(IReadOnlyDictionary<string, FriendDto> friendsCache)
    {
        var lobby = await client.GetAsync<LobbyDto>("/lol-lobby/v2/lobby");

        if (lobby?.LocalMember?.Puuid == null || lobby.Members == null)
            return null;

        var localPuuid = lobby.LocalMember.Puuid;

        var lobbyFriends = new List<LobbyFriend>();

        foreach (var member in lobby.Members)
        {
            if (member.Puuid == null) continue;

            bool isLocalPlayer = member.Puuid == localPuuid;
            bool isFriend      = friendsCache.ContainsKey(member.Puuid);

            if (!isLocalPlayer && !isFriend)
                continue;

            string gameName;
            string gameTag;

            if (isLocalPlayer)
            {
                gameName = "You";
                gameTag  = "";
            }
            else
            {
                var friend = friendsCache[member.Puuid];
                gameName = friend.GameName ?? member.Puuid;
                gameTag  = friend.GameTag  ?? "";
            }

            lobbyFriends.Add(new LobbyFriend(
                Puuid:      member.Puuid,
                SummonerId: member.SummonerId ?? 0,
                GameName:   gameName,
                GameTag:    gameTag,
                IsLeader:   member.IsLeader ?? false
            ));
        }

        return new LobbyState(localPuuid, lobbyFriends);
    }
}
