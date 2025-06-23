using System;

namespace Common.Models.Database.Interfaces;

public interface ITimeChangesTrackable
{
    DateTime CreatedTime { get; set; }
    DateTime ModifiedTime { get; set; }
}