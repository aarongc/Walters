if (BridgeTalk.appName == "photoshop") {
    try {
        app.colorSettingsPath  = "/Users/" + $.getenv("USERNAME") + "/AppData/Roaming/Adobe/Color/Settings/Book_ColorSettings.csf";
        app.colorSettings = "Book_ColorSettings";
    } catch (e) {
        app.colorSettings = "Monitor Color";
    }
}