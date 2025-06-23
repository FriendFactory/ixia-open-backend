namespace Common.Models.Database.Interfaces;

public interface IUploadedByUser
{
    long UploaderUserId { get; set; }
}