using System;

namespace TbfCa.SoundPlayRequest
{
    [Serializable]
    public class SoundPlayRequestWithName : ISoundPlayRequest
    {
        public string CueSheetName = null!;
        public string CueName = null!;
    }
}