namespace CreateAudioFingerprint
{
    public interface IFingerprintCreator
    {
        string GetForce();
        int Create(string s);
    }
}