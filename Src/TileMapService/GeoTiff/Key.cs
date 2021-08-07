namespace TileMapService.GeoTiff
{
    enum Key
    {
        // 6.2.1 GeoTIFF Configuration Keys
        GTModelTypeGeoKey = 1024,
        GTRasterTypeGeoKey = 1025,
        GTCitationGeoKey = 1026,

        // 6.2.2 Geographic CS Parameter Keys
        GeographicTypeGeoKey = 2048,
        GeogCitationGeoKey = 2049, // documentation
        GeogGeodeticDatumGeoKey = 2050,
        GeogPrimeMeridianGeoKey = 2051,
        GeogLinearUnitsGeoKey = 2052,
        GeogLinearUnitSizeGeoKey = 2053, // meters
        GeogAngularUnitsGeoKey = 2054,
        GeogSemiMajorAxisGeoKey = 2057, // GeogLinearUnits  
        GeogSemiMinorAxisGeoKey = 2058, // GeogLinearUnits      
        GeogInvFlatteningGeoKey = 2059,
        GeogAzimuthUnitsGeoKey = 2060,
        GeogPrimeMeridianLongGeoKey = 2061,

        // 6.2.3 Projected CS Parameter Keys
        ProjectedCSTypeGeoKey = 3072,
        ProjCoordTransGeoKey = 3075,
        ProjLinearUnitsGeoKey = 3076,
        ProjLinearUnitSizeGeoKey = 3077,
        ProjStdParallel1GeoKey = 3078,
        ProjStdParallel2GeoKey = 3079,
        ProjNatOriginLongGeoKey = 3080,
        ProjNatOriginLatGeoKey = 3081,
        ProjFalseEastingGeoKey = 3082,
        ProjFalseNorthingGeoKey = 3083,
        ProjFalseOriginLongGeoKey = 3084,
        ProjFalseOriginLatGeoKey = 3085,
        ProjFalseOriginEastingGeoKey = 3086,
        ProjFalseOriginNorthingGeoKey = 3087,
        ProjCenterLongGeoKey = 3088,
        ProjCenterLatGeoKey = 3089,
        ProjCenterEastingGeoKey = 3090,
        ProjCenterNorthingGeoKey = 3091,
        ProjScaleAtNatOriginGeoKey = 3092,
        ProjScaleAtCenterGeoKey = 3093,
        ProjAzimuthAngleGeoKey = 3094,
        ProjStraightVertPoleLongGeoKey = 3095,

        // 6.2.4 Vertical CS Keys
        VerticalCSTypeGeoKey = 4096,
        VerticalCitationGeoKey = 4097,
        VerticalDatumGeoKey = 4098,
        VerticalUnitsGeoKey = 4099,
    }
}
