namespace JellyTune.Shared.Enums;

public enum StartupState
{
    None,
    InitialRun,
    AccountProblem,
    MissingCollection,
    Finished,
    RequirePassword,
    SelectCollection,
    InvalidServer
}