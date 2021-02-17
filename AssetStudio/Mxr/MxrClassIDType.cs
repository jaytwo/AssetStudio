﻿namespace AssetStudio.Mxr
{
    public enum MxrClassIDType
    {
        UnknownType = -1,
        Model = 0,
        Texture = 1,
        Bitmap = 2,
        Text = 3,
        Wave = 4,
        Midi = 5,
        Camera = 7,
        Light = 8,
        Movie = 9,
        Sound3d = 11,
        Ear = 13,
        Score = 32,
        Script = 255 // Should be 6!
    }
}