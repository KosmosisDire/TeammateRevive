namespace TeammateRevive.Configuration;

public class EntryMetadata
{
    public bool RestartRequired { get; set; }

    public EntryMetadata(bool restartRequired)
    {
        this.RestartRequired = restartRequired;
    }

    public EntryMetadata()
    {
    }
}