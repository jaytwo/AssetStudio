namespace AssetStudio.Mxr.Fields
{
    enum LightField
    {
        LightType = 16,
        Colour = 17,
        Channels = 21,
        CutOffAngle = 22,
        Distance = 23,
        Tag = 24,
        CutOffAnglePhi = 25,
        ChannelList = 26,
        DropOffRate = 27,
    }

    enum LightType
    {
        Parallel = 0,
        Point = 1,
        Spot = 2,
        Ambient = 3,
    }
}