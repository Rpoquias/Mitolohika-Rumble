using System;
using System.Collections.Generic;

public interface IStandingsProvider
{
    event Action StandingsChanged;

    string SecondaryHeaderLabel { get; }

    bool IsReady { get; }

    void GetStandings(
        List<StandingEntry> buffer
    );
}