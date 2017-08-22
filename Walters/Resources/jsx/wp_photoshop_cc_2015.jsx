if (BridgeTalk.appName == "photoshop") {
    loadColorSettings();

    function loadColorSettings() {  
        var desc = new ActionDescriptor();  
        var ref = new ActionReference();  
        ref.putProperty( charIDToTypeID( "Prpr" ), stringIDToTypeID( "colorSettings" ) );  
        ref.putEnumerated( charIDToTypeID( "capp" ), charIDToTypeID( "Ordn" ), charIDToTypeID( "Trgt" ) );  
        desc.putReference( charIDToTypeID( "null" ), ref );  
        var colorSettingsDesc = new ActionDescriptor();  
        colorSettingsDesc.putPath( charIDToTypeID( "Usng" ), new File( "/Users/" + $.getenv("USERNAME") + "/AppData/Roaming/Adobe/Color/Settings/Book_ColorSettings.csf" ) );
        desc.putObject( charIDToTypeID( "T   " ), stringIDToTypeID( "colorSettings" ), colorSettingsDesc );  
        executeAction( charIDToTypeID( "setd" ), desc, DialogModes.NO );  
    }  
}