using System;
using CriWare.Assets;

namespace TbfCa.SoundPlayRequest
{
    [Serializable]
    public class SoundPlayRequestWithReference : ISoundPlayRequest
    {
        public CriAtomCueReference Reference;

        public bool IsSamePlayRequest(SoundPlayRequestWithReference other)
        {
            return Reference.AcbAsset == other.Reference.AcbAsset &&
                   Reference.CueId == other.Reference.CueId;
        }
    }
}