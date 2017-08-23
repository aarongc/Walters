if (app.name == "Adobe InDesign") {
    try {
        app.colorSettings.cmsSettingsPath  = "/Users/" + $.getenv("USERNAME") + "/AppData/Roaming/Adobe/Color/Settings/Book_ColorSettings.csf";
        app.colorSettings.cmsSettings = "Book_ColorSettings";
    } catch (e) {
        app.colorSettings.cmsSettings = "Monitor Color";
    }
}
