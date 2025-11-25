namespace JellyTune.Gnome.MediaPlayer;

class Subscription : IDisposable
{
    private readonly Action _unsubscribe;
    public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;
    public void Dispose() => _unsubscribe();
}