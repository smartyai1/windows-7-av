namespace Av.Quarantine;

public interface IUserConfirmation
{
    bool ConfirmRestore(QuarantineRecord record, string destinationPath);
}
