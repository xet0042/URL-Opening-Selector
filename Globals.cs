using System.Collections.ObjectModel;

namespace URL_Opening_Selector;

public class Globals
{
    public static readonly AppConfiguration AppConfiguration = new();
    public static bool Initialed = false;
    public static ObservableCollection<Log> Logs = new();
}