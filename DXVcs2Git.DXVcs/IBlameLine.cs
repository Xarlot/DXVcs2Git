using System;

namespace DXVcsTools.Data {
    public interface IBlameLine {
        string SourceLine { get; }
        string Comment { get; }
        string User { get; }
        int Revision { get; }
        DateTime CommitDate { get; }
    }
}