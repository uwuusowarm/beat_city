public enum GrabMode
{
    Proximity,
    PunchKick,
    PunchDirection
}

public static class ControlSettings
{
    public static GrabMode GrabMode = GrabMode.Proximity;
    public static string BindingsJson = "";
}
