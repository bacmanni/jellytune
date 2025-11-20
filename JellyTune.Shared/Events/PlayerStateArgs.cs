using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;

namespace JellyTune.Shared.Events;

public class PlayerStateArgs(
    PlayerState state,
    Album? album = null,
    List<Track>? tracks = null,
    Track? selectedTrack = null)
    : EventArgs
{
    /// <summary>
    /// Album that state is connected to
    /// </summary>
    public Album? Album { get; private set; } = album;

    /// <summary>
    /// Track that state is connected to
    /// </summary>
    public List<Track> Tracks { get; private set; } = tracks ?? [];

    /// <summary>
    /// State realates to this track guid
    /// </summary>
    public Track? SelectedTrack { get; private set; } = selectedTrack;

    /// <summary>
    /// State of the player
    /// </summary>
    public PlayerState State { get; private set; } = state;
}