namespace TeammateRevive.Configuration;

public class EntryMetadata
{
    public bool RestartRequired { get; set; }

    public EntryMetadata(bool restartRequired)
    {
        RestartRequired = restartRequired;
    }

    public EntryMetadata()
    {
    }
}